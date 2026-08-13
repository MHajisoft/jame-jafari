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
    [RequirePermission(
        PermissionCodes.PersonsView,
        PermissionCodes.IncomeView,
        PermissionCodes.IncomeCreate,
        PermissionCodes.IncomeUpdate)]
    public async Task<ActionResult<PagedResult<PersonDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] Gender? gender,
        [FromQuery, Range(1, 100)] int page = 1,
        [FromQuery, Range(1, 200)] int pageSize = 20)
        => Ok(await service.GetPagedAsync(search, gender, page, pageSize));

    [HttpGet("{id:int}")]
    [RequirePermission(
        PermissionCodes.PersonsView,
        PermissionCodes.IncomeView,
        PermissionCodes.IncomeCreate,
        PermissionCodes.IncomeUpdate)]
    public async Task<ActionResult<PersonDto>> GetById(int id)
    {
        var item = await service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.PersonsCreate)]
    public async Task<ActionResult<PersonDto>> Create([FromBody] CreatePersonRequest request)
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
    [RequirePermission(PermissionCodes.PersonsUpdate)]
    public async Task<ActionResult<PersonDto>> Update(int id, [FromBody] UpdatePersonRequest request)
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

    [HttpDelete("{id:int}")]
    [RequirePermission(PermissionCodes.PersonsDelete)]
    public async Task<IActionResult> Delete(int id)
        => await service.DeleteAsync(id, CurrentUserId) ? NoContent() : NotFound();

    [HttpPost("{id:int}/picture")]
    [RequirePermission(PermissionCodes.PersonsUpdate, PermissionCodes.PersonsCreate)]
    public async Task<ActionResult<PersonDto>> UploadPicture(int id, IFormFile file, [FromServices] FileStorageService storage)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "فایل الزامی است" });
        if (!FileStorageService.IsImageUpload(file))
            return BadRequest(new { message = "فقط تصویر مجاز است" });

        var existing = await service.GetByIdAsync(id);
        if (existing is null) return NotFound();
        var oldPath = existing.PicturePath;

        try
        {
            var path = await storage.SaveAsync(file, "persons", ImageProcessProfile.Avatar);
            var person = await service.UpdatePictureAsync(id, path, CurrentUserId);
            if (person is null)
            {
                storage.TryDelete(path);
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(oldPath) &&
                !string.Equals(oldPath, path, StringComparison.OrdinalIgnoreCase))
                storage.TryDelete(oldPath);

            return Ok(person);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}/picture")]
    [RequirePermission(PermissionCodes.PersonsUpdate)]
    public async Task<ActionResult<PersonDto>> RemovePicture(int id, [FromServices] FileStorageService storage)
    {
        var existing = await service.GetByIdAsync(id);
        if (existing is null) return NotFound();
        var oldPath = existing.PicturePath;

        var person = await service.UpdatePictureAsync(id, null, CurrentUserId);
        if (person is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(oldPath))
            storage.TryDelete(oldPath);

        return Ok(person);
    }
}
