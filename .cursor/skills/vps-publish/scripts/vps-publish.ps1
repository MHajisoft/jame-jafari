#Requires -Version 5.1
<#
.SYNOPSIS
  SSH to the test VPS, git pull, docker compose up -d --build.

.DESCRIPTION
  Reads ../config.local.md (YAML-like keys). Prefers SSH key; falls back to password
  via SSH_ASKPASS, plink, or WSL sshpass.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$skillDir = Split-Path $PSScriptRoot -Parent
$configPath = Join-Path $skillDir 'config.local.md'
if (-not (Test-Path $configPath)) {
  throw "Missing $configPath — copy config.example.md to config.local.md and fill values."
}

function Get-VpsConfig {
  param([string]$Path)
  $map = @{}
  $text = Get-Content -LiteralPath $Path -Raw
  foreach ($name in @('host', 'user', 'password', 'path', 'ssh_key')) {
    if ($text -match "(?m)^\s*${name}\s*:\s*(.+?)\s*$") {
      $val = $Matches[1].Trim().Trim('"').Trim("'")
      $map[$name] = $val
    }
  }
  foreach ($req in @('host', 'user', 'path')) {
    if ([string]::IsNullOrWhiteSpace($map[$req])) {
      throw "config.local.md missing required key: $req"
    }
  }
  return $map
}

function Invoke-Remote {
  param(
    [hashtable]$Cfg,
    [string]$RemoteCommand
  )
  $target = "$($Cfg.user)@$($Cfg.host)"
  $sshArgs = @(
    '-o', 'StrictHostKeyChecking=accept-new',
    '-o', 'ConnectTimeout=20'
  )
  if (-not [string]::IsNullOrWhiteSpace($Cfg.ssh_key)) {
    $sshArgs += @('-i', $Cfg.ssh_key)
  }

  # 1) Key / agent (non-interactive)
  $keyArgs = $sshArgs + @('-o', 'BatchMode=yes', $target, $RemoteCommand)
  & ssh @keyArgs
  if ($LASTEXITCODE -eq 0) { return }

  $hasPass = -not [string]::IsNullOrWhiteSpace($Cfg.password)
  if (-not $hasPass) {
    throw "SSH key auth failed and no password in config.local.md. Fix keys or set password."
  }

  # 2) Windows OpenSSH ASKPASS
  $askpass = Join-Path $env:TEMP ("vps-askpass-{0}.cmd" -f [guid]::NewGuid().ToString('N'))
  try {
    $passEscaped = $Cfg.password -replace '"', '""'
    Set-Content -LiteralPath $askpass -Value "@echo off`r`necho $passEscaped" -Encoding ASCII
    $env:SSH_ASKPASS = $askpass
    $env:SSH_ASKPASS_REQUIRE = 'force'
    if (-not $env:DISPLAY) { $env:DISPLAY = '1' }
    $passArgs = $sshArgs + @('-o', 'BatchMode=no', $target, $RemoteCommand)
    & ssh @passArgs
    if ($LASTEXITCODE -eq 0) { return }
  }
  finally {
    Remove-Item -LiteralPath $askpass -Force -ErrorAction SilentlyContinue
    Remove-Item Env:SSH_ASKPASS -ErrorAction SilentlyContinue
    Remove-Item Env:SSH_ASKPASS_REQUIRE -ErrorAction SilentlyContinue
  }

  # 3) plink
  $plink = Get-Command plink -ErrorAction SilentlyContinue
  if ($plink) {
    & plink -ssh $target -pw $Cfg.password -batch $RemoteCommand
    if ($LASTEXITCODE -eq 0) { return }
  }

  # 4) WSL sshpass
  $wsl = Get-Command wsl -ErrorAction SilentlyContinue
  if ($wsl) {
    $bashCmd = "sshpass -p $($Cfg.password -replace "'", "'\\''") ssh -o StrictHostKeyChecking=accept-new $target $($RemoteCommand -replace "'", "'\\''")"
    & wsl bash -lc $bashCmd
    if ($LASTEXITCODE -eq 0) { return }
  }

  throw "All SSH auth methods failed for $target"
}

$cfg = Get-VpsConfig -Path $configPath
$path = $cfg.path.TrimEnd('/')

$remote = @"
set -euo pipefail
cd '$path'
echo '== git =='
git fetch --all
git pull --ff-only
echo '== docker compose up --build =='
docker compose up -d --build
echo '== status =='
docker compose ps
echo '== HEAD =='
git log -1 --oneline
"@

Write-Host "Deploying to $($cfg.user)@$($cfg.host):$path ..."
Invoke-Remote -Cfg $cfg -RemoteCommand $remote
Write-Host "Done."
