using System.ComponentModel.DataAnnotations;
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
        [FromQuery, Range(1, 100)] int page = 1, [FromQuery, Range(1, 200)] int pageSize = 20)
        => Ok(await service.GetIncomePagedAsync(from, to, accountId, page, pageSize));

    [HttpPost]
    [RequirePermission(PermissionCodes.IncomeCreate)]
    public async Task<ActionResult<IncomeTransactionDto>> Create([FromForm] string data, [FromForm] IFormFile? document)
    {
        if (string.IsNullOrWhiteSpace(data))
            return BadRequest("داده ارسالی نامعتبر است");

        var request = JsonSerializer.Deserialize<CreateIncomeTransactionRequest>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (request is null)
            return BadRequest("داده ارسالی نامعتبر است");

        string? path = null;
        if (document is not null)
        {
            try { path = await storage.SaveAsync(document, "transactions"); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }
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
        [FromQuery, Range(1, 100)] int page = 1, [FromQuery, Range(1, 200)] int pageSize = 20)
        => Ok(await service.GetCostPagedAsync(from, to, accountId, page, pageSize));

    [HttpPost]
    [RequirePermission(PermissionCodes.CostCreate)]
    public async Task<ActionResult<CostTransactionDto>> Create([FromForm] string data, [FromForm] IFormFile? document)
    {
        if (string.IsNullOrWhiteSpace(data))
            return BadRequest("داده ارسالی نامعتبر است");

        var request = JsonSerializer.Deserialize<CreateCostTransactionRequest>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (request is null)
            return BadRequest("داده ارسالی نامعتبر است");

        string? path = null;
        if (document is not null)
        {
            try { path = await storage.SaveAsync(document, "transactions"); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }
        return Ok(await service.CreateCostAsync(request, CurrentUserId, path));
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PermissionCodes.CostDelete)]
    public async Task<IActionResult> Delete(int id)
        => await service.DeleteCostAsync(id, CurrentUserId) ? NoContent() : NotFound();
}
