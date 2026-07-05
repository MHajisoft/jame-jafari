using JameJafari.Api.Authorization;
using JameJafari.Core.Constants;
using JameJafari.Core.DTOs;
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
    public async Task<ActionResult<IReadOnlyList<AccountBalanceReportDto>>> AccountBalances(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Ok(await service.GetAccountBalancesAsync(from, to));

    [HttpGet("cost-types")]
    [RequirePermission(PermissionCodes.ReportsView)]
    public async Task<ActionResult<IReadOnlyList<CostTypeReportDto>>> CostTypes(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Ok(await service.GetCostTypeReportAsync(from, to));

    [HttpGet("summary")]
    [RequirePermission(PermissionCodes.ReportsView)]
    public async Task<ActionResult<DateRangeReportDto>> Summary([FromQuery] DateTime from, [FromQuery] DateTime to)
        => Ok(await service.GetSummaryAsync(from, to));

    [HttpGet("person-income")]
    [RequirePermission(PermissionCodes.ReportsView)]
    public async Task<ActionResult<IReadOnlyList<PersonIncomeReportDto>>> PersonIncome(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Ok(await service.GetPersonIncomeReportAsync(from, to));

    [HttpGet("food-costs")]
    [RequirePermission(PermissionCodes.ReportsView)]
    public async Task<ActionResult<IReadOnlyList<FoodCostReportDto>>> FoodCosts(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Ok(await service.GetFoodCostReportAsync(from, to));
}
