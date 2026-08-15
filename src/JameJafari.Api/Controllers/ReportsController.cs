using JameJafari.Api.Authorization;
using JameJafari.Core.Constants;
using JameJafari.Core.DTOs;
using JameJafari.Core.Enums;
using JameJafari.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JameJafari.Api.Controllers;

[Authorize]
[Route("api/reports")]
public class ReportsController(ReportService service) : ApiControllerBase
{
    [HttpGet("account-balances")]
    [RequirePermission(PermissionCodes.ReportsView)]
    public async Task<ActionResult<IReadOnlyList<AccountBalanceReportResponse>>> AccountBalances(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Ok(await service.GetAccountBalancesAsync(from, to));

    [HttpGet("cost-types")]
    [RequirePermission(PermissionCodes.ReportsView)]
    public async Task<ActionResult<IReadOnlyList<CostTypeReportResponse>>> CostTypes(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Ok(await service.GetCostTypeReportAsync(from, to));

    [HttpGet("summary")]
    [RequirePermission(PermissionCodes.ReportsView)]
    public async Task<ActionResult<DateRangeReportResponse>> Summary([FromQuery] DateTime from, [FromQuery] DateTime to)
        => Ok(await service.GetSummaryAsync(from, to));

    [HttpGet("person-income")]
    [RequirePermission(PermissionCodes.ReportsView)]
    public async Task<ActionResult<IReadOnlyList<PersonIncomeReportResponse>>> PersonIncome(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Ok(await service.GetPersonIncomeReportAsync(from, to));

    [HttpGet("food-costs")]
    [RequirePermission(PermissionCodes.ReportsView)]
    public async Task<ActionResult<IReadOnlyList<FoodCostReportResponse>>> FoodCosts(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Ok(await service.GetFoodCostReportAsync(from, to));

    [HttpGet("death-anniversaries")]
    [RequirePermission(PermissionCodes.DeathAnniversariesView)]
    public async Task<ActionResult<DeathAnniversaryReportResponse>> DeathAnniversaries(
        [FromQuery] DeathAnniversaryScope scope = DeathAnniversaryScope.Day,
        [FromQuery] DateTime? referenceDate = null)
        => Ok(await service.GetDeathAnniversaryReportAsync(scope, referenceDate));
}
