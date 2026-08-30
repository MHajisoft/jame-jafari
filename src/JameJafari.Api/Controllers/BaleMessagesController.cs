using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using JameJafari.Api.Authorization;
using JameJafari.Api.Services;
using JameJafari.Core.Constants;
using JameJafari.Core.DTOs;
using JameJafari.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JameJafari.Api.Controllers;

[Authorize]
[Route("api/bale-messages")]
public class BaleMessagesController(
    BaleMessageService service,
    BaleContactSyncService syncService,
    FileStorageService storage) : ApiControllerBase
{
    private static readonly JsonSerializerOptions FormJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [HttpGet("config")]
    [RequirePermission(PermissionCodes.MessagesView, PermissionCodes.MessagesSend)]
    public async Task<ActionResult<BaleConfigResponse>> GetConfig() => Ok(await service.GetConfigAsync());

    [HttpGet]
    [RequirePermission(PermissionCodes.MessagesView)]
    public async Task<ActionResult<PagedResult<BaleMessageResponse>>> GetAll(
        [FromQuery, Range(1, 100)] int page = 1,
        [FromQuery, Range(1, 200)] int pageSize = 20)
    {
        var result = await service.GetPagedAsync(page, pageSize);
        return Ok(ResponseVisibility.Apply(result, User));
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.MessagesSend)]
    public async Task<ActionResult<SendMessageResultResponse>> Send(
        [FromForm] string data,
        [FromForm] IFormFile? photo,
        [FromForm] IFormFileCollection? files)
    {
        if (string.IsNullOrWhiteSpace(data))
            return BadRequest("داده ارسالی نامعتبر است");

        var request = JsonSerializer.Deserialize<SendBaleMessageRequest>(data, FormJsonOptions);
        if (request is null)
            return BadRequest("داده ارسالی نامعتبر است");

        var attachmentPaths = new List<string>();
        try
        {
            if (files is { Count: > 0 })
            {
                foreach (var file in files)
                    attachmentPaths.Add(await storage.SaveAsync(file, "bale"));
            }
            else if (photo is not null)
            {
                attachmentPaths.Add(await storage.SaveAsync(photo, "bale"));
            }
        }
        catch (InvalidOperationException ex)
        {
            foreach (var path in attachmentPaths)
                storage.TryDelete(path);
            return BadRequest(new { message = ex.Message });
        }

        try
        {
            var result = await service.SendAsync(request, CurrentUserId, attachmentPaths);
            if (result.Message is not null)
            {
                return Ok(new SendMessageResultResponse
                {
                    Message = ResponseVisibility.Apply(result.Message, User),
                    Batch = result.Batch
                });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [RequirePermission(PermissionCodes.MessagesUpdate)]
    public async Task<ActionResult<BaleMessageResponse>> Update(int id, [FromBody] UpdateBaleMessageRequest request)
    {
        try
        {
            var result = await service.UpdateAsync(id, request, CurrentUserId);
            if (result is null) return NotFound();
            return Ok(ResponseVisibility.Apply(result, User));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PermissionCodes.MessagesDelete)]
    public async Task<IActionResult> Delete(int id, [FromQuery] string scope = "remote")
    {
        if (!string.Equals(scope, "remote", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(scope, "local", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "مقدار scope باید remote یا local باشد" });

        try
        {
            var result = await service.DeleteAsync(id, scope, CurrentUserId);
            if (result is null)
                return NotFound();
            foreach (var path in result.AttachmentPathsToDelete)
                storage.TryDelete(path);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("sync-contacts")]
    [RequirePermission(PermissionCodes.MessagesSend)]
    public async Task<ActionResult<object>> SyncContacts(CancellationToken cancellationToken)
    {
        try
        {
            var linked = await syncService.SyncFromUpdatesAsync(cancellationToken);
            return Ok(new { linked });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
