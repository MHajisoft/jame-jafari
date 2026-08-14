using System.ComponentModel.DataAnnotations;
using JameJafari.Api.Authorization;
using JameJafari.Api.Services;
using JameJafari.Core.Constants;
using JameJafari.Core.DTOs;
using JameJafari.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JameJafari.Api.Controllers;

[Authorize]
[Route("api/users")]
public class UsersController(UserService service, ResponseVisibilityService visibility) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.UsersView)]
    public async Task<ActionResult<PagedResult<UserDto>>> GetAll([FromQuery, Range(1, 100)] int page = 1, [FromQuery, Range(1, 200)] int pageSize = 20)
        => Ok(visibility.ForResponse(await service.GetPagedAsync(page, pageSize), User));

    [HttpGet("{id:int}")]
    [RequirePermission(PermissionCodes.UsersView)]
    public async Task<ActionResult<UserDto>> GetById(int id)
    {
        var item = await service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(visibility.ForResponse(item, User));
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.UsersCreate)]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request)
    {
        try
        {
            return Ok(visibility.ForResponse(await service.CreateAsync(request, CurrentUserId), User));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [RequirePermission(PermissionCodes.UsersUpdate)]
    public async Task<ActionResult<UserDto>> Update(int id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var item = await service.UpdateAsync(id, request, CurrentUserId);
            return item is null ? NotFound() : Ok(visibility.ForResponse(item, User));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/password")]
    [RequirePermission(PermissionCodes.UsersChangePassword)]
    public async Task<ActionResult<UserDto>> ChangePassword(int id, [FromBody] ChangeUserPasswordRequest request)
    {
        try
        {
            var item = await service.ChangePasswordAsync(id, request, CurrentUserId);
            return item is null ? NotFound() : Ok(visibility.ForResponse(item, User));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PermissionCodes.UsersDelete)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            return await service.DeleteAsync(id, CurrentUserId) ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/avatar")]
    [RequirePermission(PermissionCodes.UsersUpdate, PermissionCodes.UsersCreate)]
    public async Task<ActionResult<UserDto>> UploadAvatar(int id, IFormFile file, [FromServices] FileStorageService storage)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "فایل الزامی است" });
        if (!FileStorageService.IsImageUpload(file))
            return BadRequest(new { message = "فقط تصویر مجاز است" });

        var existing = await service.GetByIdAsync(id);
        if (existing is null) return NotFound();
        var oldPath = existing.AvatarPath;

        try
        {
            var path = await storage.SaveAsync(file, "avatars", ImageProcessProfile.Avatar);
            var user = await service.UpdateAvatarAsync(id, path, CurrentUserId);
            if (user is null)
            {
                storage.TryDelete(path);
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(oldPath) &&
                !string.Equals(oldPath, path, StringComparison.OrdinalIgnoreCase))
                storage.TryDelete(oldPath);

            return Ok(visibility.ForResponse(user, User));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}/avatar")]
    [RequirePermission(PermissionCodes.UsersUpdate)]
    public async Task<ActionResult<UserDto>> RemoveAvatar(int id, [FromServices] FileStorageService storage)
    {
        try
        {
            var existing = await service.GetByIdAsync(id);
            if (existing is null) return NotFound();
            var oldPath = existing.AvatarPath;

            var user = await service.UpdateAvatarAsync(id, null, CurrentUserId);
            if (user is null) return NotFound();

            if (!string.IsNullOrWhiteSpace(oldPath))
                storage.TryDelete(oldPath);

            return Ok(visibility.ForResponse(user, User));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
