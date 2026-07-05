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
    public async Task<ActionResult<IReadOnlyList<CostTypeDto>>> GetAll([FromQuery] bool? isIngredient)
        => Ok(await service.GetAllAsync(isIngredient));

    [HttpPost]
    [RequirePermission(PermissionCodes.CostTypesManage)]
    public async Task<ActionResult<CostTypeDto>> Create([FromBody] CreateCostTypeRequest request)
        => Ok(await service.CreateAsync(request, CurrentUserId));

    [HttpPut("{id:int}")]
    [RequirePermission(PermissionCodes.CostTypesManage)]
    public async Task<ActionResult<CostTypeDto>> Update(int id, [FromBody] UpdateCostTypeRequest request)
    {
        var item = await service.UpdateAsync(id, request, CurrentUserId);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PermissionCodes.CostTypesManage)]
    public async Task<IActionResult> Delete(int id)
        => await service.DeleteAsync(id, CurrentUserId) ? NoContent() : NotFound();
}
