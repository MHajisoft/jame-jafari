using JameJafari.Api.Authorization;
using JameJafari.Core.Constants;
using JameJafari.Core.DTOs;
using JameJafari.Core.Enums;
using JameJafari.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JameJafari.Api.Controllers;

/// <summary>
/// Read-only reference data for forms (select lists). Narrower than admin CRUD list APIs.
/// </summary>
[Authorize]
[Route("api/lookups")]
public class LookupsController(LookupService service) : ApiControllerBase
{
    [HttpGet("accounts")]
    [RequirePermission(
        PermissionCodes.AccountsView,
        PermissionCodes.IncomeView,
        PermissionCodes.IncomeCreate,
        PermissionCodes.IncomeUpdate,
        PermissionCodes.CostView,
        PermissionCodes.CostCreate,
        PermissionCodes.CostUpdate)]
    public async Task<ActionResult<IReadOnlyList<LookupItemDto>>> GetAccounts([FromQuery] bool activeOnly = true)
        => Ok(await service.GetAccountsAsync(activeOnly));

    [HttpGet("cost-types")]
    [RequirePermission(
        PermissionCodes.CostTypesView,
        PermissionCodes.IncomeView,
        PermissionCodes.IncomeCreate,
        PermissionCodes.IncomeUpdate,
        PermissionCodes.CostView,
        PermissionCodes.CostCreate,
        PermissionCodes.CostUpdate,
        PermissionCodes.FoodView,
        PermissionCodes.FoodCreate,
        PermissionCodes.FoodUpdate)]
    public async Task<ActionResult<IReadOnlyList<CostTypeLookupItemDto>>> GetCostTypes(
        [FromQuery] bool? isIngredient,
        [FromQuery] bool activeOnly = true)
        => Ok(await service.GetCostTypesAsync(isIngredient, activeOnly));

    [HttpGet("general-types")]
    [RequirePermission(
        PermissionCodes.GeneralTypesView,
        PermissionCodes.PersonsView,
        PermissionCodes.PersonsCreate,
        PermissionCodes.PersonsUpdate,
        PermissionCodes.CostTypesView,
        PermissionCodes.CostTypesCreate,
        PermissionCodes.CostTypesUpdate,
        PermissionCodes.IncomeView,
        PermissionCodes.IncomeCreate,
        PermissionCodes.IncomeUpdate,
        PermissionCodes.CostView,
        PermissionCodes.CostCreate,
        PermissionCodes.CostUpdate,
        PermissionCodes.FoodView,
        PermissionCodes.FoodCreate,
        PermissionCodes.FoodUpdate)]
    public async Task<ActionResult<IReadOnlyList<LookupItemDto>>> GetGeneralTypes([FromQuery] string category)
    {
        if (!Enum.TryParse<GeneralTypeCategory>(category, true, out var cat))
            return BadRequest("Invalid category");
        return Ok(await service.GetGeneralTypesAsync(cat));
    }
}
