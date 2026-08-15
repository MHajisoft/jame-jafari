using System.ComponentModel.DataAnnotations;
using JameJafari.Api.Authorization;
using JameJafari.Api.Services;
using JameJafari.Core.Constants;
using JameJafari.Core.DTOs;
using JameJafari.Core.Enums;
using JameJafari.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JameJafari.Api.Controllers;

[Authorize]
[Route("api/general-types")]
public class GeneralTypesController(GeneralTypeService service) : ApiControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<PagedResult<GeneralTypeResponse>>> GetByCategory(
        [FromQuery] string category,
        [FromQuery] bool includeInactive = false,
        [FromQuery, Range(1, 100)] int page = 1,
        [FromQuery, Range(1, 200)] int pageSize = 20)
    {
        if (!Enum.TryParse<GeneralTypeCategory>(category, true, out var cat))
            return BadRequest("Invalid category");
        // Inactive listing is for management screens only
        if (includeInactive)
        {
            var user = User;
            var canManage = user.HasClaim("permission", PermissionCodes.GeneralTypesView)
                || user.HasClaim("permission", PermissionCodes.GeneralTypesCreate)
                || user.HasClaim("permission", PermissionCodes.GeneralTypesUpdate)
                || user.HasClaim("permission", PermissionCodes.GeneralTypesDelete);
            if (!canManage) return Forbid();
        }
        return Ok(ResponseVisibility.Apply(
            await service.GetPagedByCategoryAsync(cat, includeInactive, page, pageSize), User));
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.GeneralTypesCreate)]
    public async Task<ActionResult<GeneralTypeResponse>> Create([FromBody] CreateGeneralTypeRequest request)
        => Ok(ResponseVisibility.Apply(await service.CreateAsync(request, CurrentUserId), User));

    [HttpPut("{id:int}")]
    [RequirePermission(PermissionCodes.GeneralTypesUpdate)]
    public async Task<ActionResult<GeneralTypeResponse>> Update(int id, [FromBody] UpdateGeneralTypeRequest request)
    {
        var item = await service.UpdateAsync(id, request, CurrentUserId);
        return item is null ? NotFound() : Ok(ResponseVisibility.Apply(item, User));
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PermissionCodes.GeneralTypesDelete)]
    public async Task<IActionResult> Delete(int id)
        => await service.DeleteAsync(id, CurrentUserId) ? NoContent() : NotFound();
}
