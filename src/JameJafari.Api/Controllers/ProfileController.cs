using JameJafari.Api.Services;
using JameJafari.Core.DTOs;
using JameJafari.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JameJafari.Api.Controllers;

[Authorize]
[Route("api/profile")]
public class ProfileController(AuthService authService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProfileResponse>> Get()
    {
        var profile = await authService.GetProfileAsync(CurrentUserId);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut]
    public async Task<ActionResult<ProfileResponse>> Update([FromBody] UpdateProfileRequest request)
    {
        var profile = await authService.UpdateProfileAsync(CurrentUserId, request);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var (ok, error) = await authService.ChangePasswordAsync(CurrentUserId, request);
        if (!ok) return BadRequest(new { message = error });
        return NoContent();
    }

    [HttpPost("avatar")]
    public async Task<ActionResult<ProfileResponse>> UploadAvatar(IFormFile file, [FromServices] FileStorageService storage)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "فایل الزامی است" });
        if (!FileStorageService.IsImageUpload(file))
            return BadRequest(new { message = "فقط تصویر مجاز است" });

        var current = await authService.GetProfileAsync(CurrentUserId);
        var oldPath = current?.AvatarPath;

        try
        {
            var path = await storage.SaveAsync(file, "avatars", ImageProcessProfile.Avatar);
            var profile = await authService.UpdateAvatarAsync(CurrentUserId, path);
            if (profile is null)
            {
                storage.TryDelete(path);
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(oldPath) &&
                !string.Equals(oldPath, path, StringComparison.OrdinalIgnoreCase))
                storage.TryDelete(oldPath);

            return Ok(profile);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("avatar")]
    public async Task<ActionResult<ProfileResponse>> RemoveAvatar([FromServices] FileStorageService storage)
    {
        var current = await authService.GetProfileAsync(CurrentUserId);
        var oldPath = current?.AvatarPath;

        var profile = await authService.UpdateAvatarAsync(CurrentUserId, null);
        if (profile is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(oldPath))
            storage.TryDelete(oldPath);

        return Ok(profile);
    }
}
