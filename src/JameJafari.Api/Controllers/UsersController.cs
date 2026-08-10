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
        => Ok(await service.CreateAsync(request, CurrentUserId));

    [HttpPut("{id:int}")]
    [RequirePermission(PermissionCodes.UsersUpdate)]
    public async Task<ActionResult<UserDto>> Update(int id, [FromBody] UpdateUserRequest request)
    {
        var item = await service.UpdateAsync(id, request, CurrentUserId);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PermissionCodes.UsersDelete)]
    public async Task<IActionResult> Delete(int id)
        => await service.DeleteAsync(id, CurrentUserId) ? NoContent() : NotFound();

    [HttpPost("{id:int}/avatar")]
    [RequirePermission(PermissionCodes.UsersUpdate)]
    public async Task<ActionResult<UserDto>> UploadAvatar(int id, IFormFile file, [FromServices] FileStorageService storage)
    {
        var path = await storage.SaveAsync(file, "avatars");
        var user = await service.UpdateAvatarAsync(id, path, CurrentUserId);
        return user is null ? NotFound() : Ok(user);
    }
}
