using JameJafari.Api.Authorization;
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
    public async Task<ActionResult<IReadOnlyList<GeneralTypeDto>>> GetByCategory(
        [FromQuery] string category,
        [FromQuery] bool includeInactive = false)
    {
        if (!Enum.TryParse<GeneralTypeCategory>(category, true, out var cat))
            return BadRequest("Invalid category");
        return Ok(await service.GetByCategoryAsync(cat, includeInactive));
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.GeneralTypesManage, PermissionCodes.CostTypesManage)]
    public async Task<ActionResult<GeneralTypeDto>> Create([FromBody] CreateGeneralTypeRequest request)
        => Ok(await service.CreateAsync(request, CurrentUserId));

    [HttpPut("{id:int}")]
    [RequirePermission(PermissionCodes.GeneralTypesManage, PermissionCodes.CostTypesManage)]
    public async Task<ActionResult<GeneralTypeDto>> Update(int id, [FromBody] UpdateGeneralTypeRequest request)
    {
        var item = await service.UpdateAsync(id, request, CurrentUserId);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PermissionCodes.GeneralTypesManage, PermissionCodes.CostTypesManage)]
    public async Task<IActionResult> Delete(int id)
        => await service.DeleteAsync(id, CurrentUserId) ? NoContent() : NotFound();
}
