using JameJafari.Api.Authorization;
using JameJafari.Core.Constants;
using JameJafari.Core.DTOs;
using JameJafari.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JameJafari.Api.Controllers;

[Authorize]
[Route("api/cost-types")]
public class CostTypesController(CostTypeService service) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.CostTypesView)]
    public async Task<ActionResult<IReadOnlyList<CostTypeDto>>> GetAll(
        [FromQuery] bool? isIngredient,
        [FromQuery] bool activeOnly = true)
        => Ok(ApplyAuditVisibility(await service.GetAllAsync(isIngredient, activeOnly), static d => d with { Audit = NoAudit }));

    [HttpPost]
    [RequirePermission(PermissionCodes.CostTypesCreate)]
    public async Task<ActionResult<CostTypeDto>> Create([FromBody] CreateCostTypeRequest request)
        => Ok(ApplyAuditVisibility(await service.CreateAsync(request, CurrentUserId), static d => d with { Audit = NoAudit }));

    [HttpPut("{id:int}")]
    [RequirePermission(PermissionCodes.CostTypesUpdate)]
    public async Task<ActionResult<CostTypeDto>> Update(int id, [FromBody] UpdateCostTypeRequest request)
    {
        var item = await service.UpdateAsync(id, request, CurrentUserId);
        return item is null ? NotFound() : Ok(ApplyAuditVisibility(item, static d => d with { Audit = NoAudit }));
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PermissionCodes.CostTypesDelete)]
    public async Task<IActionResult> Delete(int id)
        => await service.DeleteAsync(id, CurrentUserId) ? NoContent() : NotFound();
}
