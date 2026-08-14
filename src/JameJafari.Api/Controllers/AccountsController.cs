using JameJafari.Api.Authorization;
using JameJafari.Core.Constants;
using JameJafari.Core.DTOs;
using JameJafari.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JameJafari.Api.Controllers;

[Authorize]
[Route("api/accounts")]
public class AccountsController(AccountService service) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.AccountsView)]
    public async Task<ActionResult<IReadOnlyList<AccountDto>>> GetAll([FromQuery] bool activeOnly = true)
        => Ok(ApplyAuditVisibility(await service.GetAllAsync(activeOnly), static d => d with { Audit = NoAudit }));

    [HttpGet("{id:int}")]
    [RequirePermission(PermissionCodes.AccountsView)]
    public async Task<ActionResult<AccountDto>> GetById(int id)
    {
        var item = await service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(ApplyAuditVisibility(item, static d => d with { Audit = NoAudit }));
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.AccountsCreate)]
    public async Task<ActionResult<AccountDto>> Create([FromBody] CreateAccountRequest request)
        => Ok(ApplyAuditVisibility(await service.CreateAsync(request, CurrentUserId), static d => d with { Audit = NoAudit }));

    [HttpPut("{id:int}")]
    [RequirePermission(PermissionCodes.AccountsUpdate)]
    public async Task<ActionResult<AccountDto>> Update(int id, [FromBody] UpdateAccountRequest request)
    {
        var item = await service.UpdateAsync(id, request, CurrentUserId);
        return item is null ? NotFound() : Ok(ApplyAuditVisibility(item, static d => d with { Audit = NoAudit }));
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PermissionCodes.AccountsDelete)]
    public async Task<IActionResult> Delete(int id)
        => await service.DeleteAsync(id, CurrentUserId) ? NoContent() : NotFound();
}
