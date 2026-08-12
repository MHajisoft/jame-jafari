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
public class UsersController(UserService service) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.UsersView)]
    public async Task<ActionResult<PagedResult<UserDto>>> GetAll([FromQuery, Range(1, 100)] int page = 1, [FromQuery, Range(1, 200)] int pageSize = 20)
        => Ok(await service.GetPagedAsync(page, pageSize));

    [HttpGet("{id:int}")]
    [RequirePermission(PermissionCodes.UsersView)]
    public async Task<ActionResult<UserDto>> GetById(int id)
    {
        var item = await service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.UsersCreate)]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request)
    {
        try
        {
            return Ok(await service.CreateAsync(request, CurrentUserId));
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
            return item is null ? NotFound() : Ok(item);
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
            return item is null ? NotFound() : Ok(item);
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
        try
        {
            var path = await storage.SaveAsync(file, "avatars");
            var user = await service.UpdateAvatarAsync(id, path, CurrentUserId);
            return user is null ? NotFound() : Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}/avatar")]
    [RequirePermission(PermissionCodes.UsersUpdate)]
    public async Task<ActionResult<UserDto>> RemoveAvatar(int id)
    {
        try
        {
            var user = await service.UpdateAvatarAsync(id, null, CurrentUserId);
            return user is null ? NotFound() : Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
