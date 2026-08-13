---
name: vps-publish
description: >-
  Deploy Jame-Jafari to the Linux test VPS over SSH: git pull and docker compose
  rebuild. Use when the user asks to publish, deploy, update the test server,
  or sync the VPS after a git push.
---

# VPS publish (test host)

## When to use

User asks to publish / deploy / update the test VPS after code was pushed to git.

## Config (required)

1. Read [config.local.md](config.local.md) if it exists.
2. If missing, copy [config.example.md](config.example.md) → `config.local.md` and ask the user to fill values.
3. **Never** commit `config.local.md`, print passwords, or paste secrets into chat replies.

| Key | Purpose |
|-----|---------|
| `host` | VPS IP or hostname |
| `user` | SSH user |
| `password` | Optional; prefer SSH key. Used only if key auth fails |
| `path` | Absolute app dir on VPS (repo root with `docker-compose.yml`) |
| `ssh_key` | Optional path to private key |

## Deploy steps

Run from the workspace via Shell (network + all permissions as needed).

### 1. Prefer the helper script (Windows)

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .cursor/skills/vps-publish/scripts/vps-publish.ps1
```

### 2. Or equivalent remote commands

On the VPS (via SSH), in `path`:

```bash
set -euo pipefail
cd "$VPS_PATH"
git fetch --all
git pull --ff-only
docker compose up -d --build
docker compose ps
git log -1 --oneline
```

**Do not** use bare `docker compose down` / `up` without `--build` — images will stay stale.  
**Never** run `docker compose down -v` (wipes DB + uploads).

### 3. Report back

- Remote `git log -1 --oneline`
- `docker compose ps` status
- Tail of build errors if any

## Clean rebuild (only if user asks)

```bash
docker compose build --no-cache api web
docker compose up -d
```

## Auth notes

1. Try key-based SSH first (`BatchMode=yes` or `ssh_key` from config).
2. If that fails and `password` is set, use `scripts/vps-publish.ps1` (SSH_ASKPASS / plink / WSL sshpass).
3. Strongly prefer installing an SSH public key on the server and removing the password from `config.local.md`.

## Failure handling

| Symptom | Action |
|---------|--------|
| SSH auth failed | Stop; tell user to fix key/password/firewall — do not invent credentials |
| `git pull` rejected | Stop; show status — do not force-push or reset unless user asks |
| Wrong `path` | Ask user for the real compose directory on the VPS |
| Build fails | Show logs; do not hide errors |
