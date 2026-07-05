using System.Text.Json;
using JameJafari.Api.Authorization;
using JameJafari.Core.Constants;
using JameJafari.Core.DTOs;
using JameJafari.Api.Services;
using JameJafari.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JameJafari.Api.Controllers;

[Authorize]
[Route("api/income-transactions")]
public class IncomeTransactionsController(TransactionService service, FileStorageService storage) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.IncomeView)]
    public async Task<ActionResult<PagedResult<IncomeTransactionDto>>> GetAll(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? accountId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await service.GetIncomePagedAsync(from, to, accountId, page, pageSize));

    [HttpPost]
    [RequirePermission(PermissionCodes.IncomeCreate)]
    public async Task<ActionResult<IncomeTransactionDto>> Create([FromForm] string data, [FromForm] IFormFile? document)
    {
        var request = JsonSerializer.Deserialize<CreateIncomeTransactionRequest>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        string? path = document is not null ? await storage.SaveAsync(document, "transactions") : null;
        return Ok(await service.CreateIncomeAsync(request, CurrentUserId, path));
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PermissionCodes.IncomeDelete)]
    public async Task<IActionResult> Delete(int id)
        => await service.DeleteIncomeAsync(id, CurrentUserId) ? NoContent() : NotFound();
}

[Authorize]
[Route("api/cost-transactions")]
public class CostTransactionsController(TransactionService service, FileStorageService storage) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.CostView)]
    public async Task<ActionResult<PagedResult<CostTransactionDto>>> GetAll(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? accountId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await service.GetCostPagedAsync(from, to, accountId, page, pageSize));

    [HttpPost]
    [RequirePermission(PermissionCodes.CostCreate)]
    public async Task<ActionResult<CostTransactionDto>> Create([FromForm] string data, [FromForm] IFormFile? document)
    {
        var request = JsonSerializer.Deserialize<CreateCostTransactionRequest>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        string? path = document is not null ? await storage.SaveAsync(document, "transactions") : null;
        return Ok(await service.CreateCostAsync(request, CurrentUserId, path));
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PermissionCodes.CostDelete)]
    public async Task<IActionResult> Delete(int id)
        => await service.DeleteCostAsync(id, CurrentUserId) ? NoContent() : NotFound();
}
