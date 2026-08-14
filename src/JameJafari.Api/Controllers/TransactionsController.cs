using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
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
public class IncomeTransactionsController(
    TransactionService service,
    FileStorageService storage,
    ResponseVisibilityService visibility) : ApiControllerBase
{
    private static readonly JsonSerializerOptions FormJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [HttpGet]
    [RequirePermission(
        PermissionCodes.IncomeView,
        PermissionCodes.IncomeCreate,
        PermissionCodes.IncomeUpdate)]
    public async Task<ActionResult<PagedResult<IncomeTransactionDto>>> GetAll(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? accountId,
        [FromQuery, Range(1, 100)] int page = 1, [FromQuery, Range(1, 200)] int pageSize = 20)
    {
        int? ownOnly = OwnRecordsFilter(PermissionCodes.IncomeView);
        var result = await service.GetIncomePagedAsync(from, to, accountId, page, pageSize, ownOnly);
        return Ok(visibility.ForIncomeResponse(result, User));
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.IncomeCreate)]
    public async Task<ActionResult<IncomeTransactionDto>> Create([FromForm] string data, [FromForm] IFormFileCollection? documents)
    {
        if (string.IsNullOrWhiteSpace(data))
            return BadRequest("داده ارسالی نامعتبر است");

        var request = JsonSerializer.Deserialize<CreateIncomeTransactionRequest>(data, FormJsonOptions);
        if (request is null)
            return BadRequest("داده ارسالی نامعتبر است");

        if (EnsureCanAddAttachments(documents) is { } denied)
            return denied;

        var paths = await SaveDocumentsAsync(documents);
        if (paths is null) return BadRequest(new { message = "خطا در ذخیره پیوست" });

        return Ok(visibility.ForIncomeResponse(await service.CreateIncomeAsync(request, CurrentUserId, paths), User));
    }

    [HttpPut("{id:int}")]
    [RequirePermission(PermissionCodes.IncomeUpdate)]
    public async Task<ActionResult<IncomeTransactionDto>> Update(int id, [FromForm] string data, [FromForm] IFormFileCollection? documents)
    {
        if (string.IsNullOrWhiteSpace(data))
            return BadRequest("داده ارسالی نامعتبر است");

        var request = JsonSerializer.Deserialize<UpdateIncomeTransactionRequest>(data, FormJsonOptions);
        if (request is null)
            return BadRequest("داده ارسالی نامعتبر است");

        if (EnsureCanAddAttachments(documents) is { } denied)
            return denied;

        var paths = await SaveDocumentsAsync(documents);
        if (paths is null) return BadRequest(new { message = "خطا در ذخیره پیوست" });

        var updated = await service.UpdateIncomeAsync(
            id, request, CurrentUserId, paths, OwnRecordsFilter(PermissionCodes.IncomeView));
        return updated is null ? NotFound() : Ok(visibility.ForIncomeResponse(updated, User));
    }

    [HttpDelete("{id:int}/attachments/{attachmentId:int}")]
    [RequirePermission(PermissionCodes.AttachmentsDelete)]
    public async Task<IActionResult> DeleteAttachment(int id, int attachmentId)
    {
        if (!HasPermission(PermissionCodes.IncomeUpdate))
            return Forbid();

        var ownOnly = OwnRecordsFilter(PermissionCodes.IncomeView);
        var path = await service.GetIncomeAttachmentPathAsync(id, attachmentId, ownOnly);
        if (path is null) return NotFound();

        if (!await service.DeleteIncomeAttachmentAsync(id, attachmentId, ownOnly))
            return NotFound();

        storage.TryDelete(path);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PermissionCodes.IncomeDelete)]
    public async Task<IActionResult> Delete(int id)
        => await service.DeleteIncomeAsync(id, CurrentUserId, OwnRecordsFilter(PermissionCodes.IncomeView))
            ? NoContent()
            : NotFound();

    private async Task<IReadOnlyList<string>?> SaveDocumentsAsync(IFormFileCollection? documents)
    {
        if (documents is null || documents.Count == 0)
            return Array.Empty<string>();

        var paths = new List<string>(documents.Count);
        foreach (var document in documents)
        {
            try
            {
                var profile = FileStorageService.IsImageUpload(document)
                    ? ImageProcessProfile.Document
                    : (ImageProcessProfile?)null;
                paths.Add(await storage.SaveAsync(document, "transactions", profile));
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }
        return paths;
    }
}

[Authorize]
[Route("api/cost-transactions")]
public class CostTransactionsController(
    TransactionService service,
    FileStorageService storage,
    ResponseVisibilityService visibility) : ApiControllerBase
{
    private static readonly JsonSerializerOptions FormJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [HttpGet]
    [RequirePermission(
        PermissionCodes.CostView,
        PermissionCodes.CostCreate,
        PermissionCodes.CostUpdate)]
    public async Task<ActionResult<PagedResult<CostTransactionDto>>> GetAll(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? accountId,
        [FromQuery, Range(1, 100)] int page = 1, [FromQuery, Range(1, 200)] int pageSize = 20)
    {
        int? ownOnly = OwnRecordsFilter(PermissionCodes.CostView);
        var result = await service.GetCostPagedAsync(from, to, accountId, page, pageSize, ownOnly);
        return Ok(visibility.ForCostResponse(result, User));
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.CostCreate)]
    public async Task<ActionResult<CostTransactionDto>> Create([FromForm] string data, [FromForm] IFormFileCollection? documents)
    {
        if (string.IsNullOrWhiteSpace(data))
            return BadRequest("داده ارسالی نامعتبر است");

        var request = JsonSerializer.Deserialize<CreateCostTransactionRequest>(data, FormJsonOptions);
        if (request is null)
            return BadRequest("داده ارسالی نامعتبر است");

        if (EnsureCanAddAttachments(documents) is { } denied)
            return denied;

        var paths = await SaveDocumentsAsync(documents);
        if (paths is null) return BadRequest(new { message = "خطا در ذخیره پیوست" });

        return Ok(visibility.ForCostResponse(await service.CreateCostAsync(request, CurrentUserId, paths), User));
    }

    [HttpPut("{id:int}")]
    [RequirePermission(PermissionCodes.CostUpdate)]
    public async Task<ActionResult<CostTransactionDto>> Update(int id, [FromForm] string data, [FromForm] IFormFileCollection? documents)
    {
        if (string.IsNullOrWhiteSpace(data))
            return BadRequest("داده ارسالی نامعتبر است");

        var request = JsonSerializer.Deserialize<UpdateCostTransactionRequest>(data, FormJsonOptions);
        if (request is null)
            return BadRequest("داده ارسالی نامعتبر است");

        if (EnsureCanAddAttachments(documents) is { } denied)
            return denied;

        var paths = await SaveDocumentsAsync(documents);
        if (paths is null) return BadRequest(new { message = "خطا در ذخیره پیوست" });

        var updated = await service.UpdateCostAsync(
            id, request, CurrentUserId, paths, OwnRecordsFilter(PermissionCodes.CostView));
        return updated is null ? NotFound() : Ok(visibility.ForCostResponse(updated, User));
    }

    [HttpDelete("{id:int}/attachments/{attachmentId:int}")]
    [RequirePermission(PermissionCodes.AttachmentsDelete)]
    public async Task<IActionResult> DeleteAttachment(int id, int attachmentId)
    {
        if (!HasPermission(PermissionCodes.CostUpdate))
            return Forbid();

        var ownOnly = OwnRecordsFilter(PermissionCodes.CostView);
        var path = await service.GetCostAttachmentPathAsync(id, attachmentId, ownOnly);
        if (path is null) return NotFound();

        if (!await service.DeleteCostAttachmentAsync(id, attachmentId, ownOnly))
            return NotFound();

        storage.TryDelete(path);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PermissionCodes.CostDelete)]
    public async Task<IActionResult> Delete(int id)
        => await service.DeleteCostAsync(id, CurrentUserId, OwnRecordsFilter(PermissionCodes.CostView))
            ? NoContent()
            : NotFound();

    private async Task<IReadOnlyList<string>?> SaveDocumentsAsync(IFormFileCollection? documents)
    {
        if (documents is null || documents.Count == 0)
            return Array.Empty<string>();

        var paths = new List<string>(documents.Count);
        foreach (var document in documents)
        {
            try
            {
                var profile = FileStorageService.IsImageUpload(document)
                    ? ImageProcessProfile.Document
                    : (ImageProcessProfile?)null;
                paths.Add(await storage.SaveAsync(document, "transactions", profile));
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }
        return paths;
    }
}
