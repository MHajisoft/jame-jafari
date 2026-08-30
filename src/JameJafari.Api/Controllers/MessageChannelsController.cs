using System.ComponentModel.DataAnnotations;
using JameJafari.Api.Authorization;
using JameJafari.Api.Services;
using JameJafari.Core.Constants;
using JameJafari.Core.DTOs;
using JameJafari.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JameJafari.Api.Controllers;

[Authorize]
[Route("api/message-channels")]
public class MessageChannelsController(MessageChannelService service) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.MessageChannelsView)]
    public async Task<ActionResult<PagedResult<MessageChannelResponse>>> GetAll(
        [FromQuery] bool activeOnly = true,
        [FromQuery, Range(1, 100)] int page = 1,
        [FromQuery, Range(1, 200)] int pageSize = 20)
        => Ok(ResponseVisibility.Apply(await service.GetPagedAsync(activeOnly, page, pageSize), User));

    [HttpGet("{id:int}")]
    [RequirePermission(PermissionCodes.MessageChannelsView)]
    public async Task<ActionResult<MessageChannelResponse>> GetById(int id)
    {
        var item = await service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(ResponseVisibility.Apply(item, User));
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.MessageChannelsCreate)]
    public async Task<ActionResult<MessageChannelResponse>> Create([FromBody] CreateMessageChannelRequest request)
        => Ok(ResponseVisibility.Apply(await service.CreateAsync(request, CurrentUserId), User));

    [HttpPut("{id:int}")]
    [RequirePermission(PermissionCodes.MessageChannelsUpdate)]
    public async Task<ActionResult<MessageChannelResponse>> Update(int id, [FromBody] UpdateMessageChannelRequest request)
    {
        var item = await service.UpdateAsync(id, request, CurrentUserId);
        return item is null ? NotFound() : Ok(ResponseVisibility.Apply(item, User));
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PermissionCodes.MessageChannelsDelete)]
    public async Task<IActionResult> Delete(int id)
        => await service.DeleteAsync(id, CurrentUserId) ? NoContent() : NotFound();
}
