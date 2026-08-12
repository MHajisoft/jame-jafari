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
    public async Task<ActionResult<ProfileDto>> Get()
    {
        var profile = await authService.GetProfileAsync(CurrentUserId);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut]
    public async Task<ActionResult<ProfileDto>> Update([FromBody] UpdateProfileRequest request)
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
    public async Task<ActionResult<ProfileDto>> UploadAvatar(IFormFile file, [FromServices] FileStorageService storage)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "فایل الزامی است" });
        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "فقط تصویر مجاز است" });

        try
        {
            var path = await storage.SaveAsync(file, "avatars");
            var profile = await authService.UpdateAvatarAsync(CurrentUserId, path);
            return profile is null ? NotFound() : Ok(profile);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("avatar")]
    public async Task<ActionResult<ProfileDto>> RemoveAvatar()
    {
        var profile = await authService.UpdateAvatarAsync(CurrentUserId, null);
        return profile is null ? NotFound() : Ok(profile);
    }
}
