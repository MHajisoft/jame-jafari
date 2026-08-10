using System.ComponentModel.DataAnnotations;
using JameJafari.Api.Authorization;
using JameJafari.Core.Constants;
using JameJafari.Core.DTOs;
using JameJafari.Core.Enums;
using JameJafari.Api.Services;
using JameJafari.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JameJafari.Api.Controllers;

[Authorize]
[Route("api/persons")]
public class PersonsController(PersonService service) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.PersonsView)]
    public async Task<ActionResult<PagedResult<PersonDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] Gender? gender,
        [FromQuery, Range(1, 100)] int page = 1,
        [FromQuery, Range(1, 200)] int pageSize = 20)
        => Ok(await service.GetPagedAsync(search, gender, page, pageSize));

    [HttpGet("{id:int}")]
    [RequirePermission(PermissionCodes.PersonsView)]
    public async Task<ActionResult<PersonDto>> GetById(int id)
    {
        var item = await service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.PersonsCreate)]
    public async Task<ActionResult<PersonDto>> Create([FromBody] CreatePersonRequest request)
        => Ok(await service.CreateAsync(request, CurrentUserId));

    [HttpPut("{id:int}")]
    [RequirePermission(PermissionCodes.PersonsUpdate)]
    public async Task<ActionResult<PersonDto>> Update(int id, [FromBody] UpdatePersonRequest request)
    {
        var item = await service.UpdateAsync(id, request, CurrentUserId);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PermissionCodes.PersonsDelete)]
    public async Task<IActionResult> Delete(int id)
        => await service.DeleteAsync(id, CurrentUserId) ? NoContent() : NotFound();

    [HttpPost("{id:int}/picture")]
    [RequirePermission(PermissionCodes.PersonsUpdate, PermissionCodes.PersonsCreate)]
    public async Task<ActionResult<PersonDto>> UploadPicture(int id, IFormFile file, [FromServices] FileStorageService storage)
    {
        var path = await storage.SaveAsync(file, "persons");
        var person = await service.UpdatePictureAsync(id, path, CurrentUserId);
        return person is null ? NotFound() : Ok(person);
    }

    [HttpDelete("{id:int}/picture")]
    [RequirePermission(PermissionCodes.PersonsUpdate)]
    public async Task<ActionResult<PersonDto>> RemovePicture(int id)
    {
        var person = await service.UpdatePictureAsync(id, null, CurrentUserId);
        return person is null ? NotFound() : Ok(person);
    }
}
