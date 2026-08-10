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
        => Ok(await service.GetAllAsync(activeOnly));

    [HttpGet("{id:int}")]
    [RequirePermission(PermissionCodes.AccountsView)]
    public async Task<ActionResult<AccountDto>> GetById(int id)
    {
        var item = await service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.AccountsCreate)]
    public async Task<ActionResult<AccountDto>> Create([FromBody] CreateAccountRequest request)
        => Ok(await service.CreateAsync(request, CurrentUserId));

    [HttpPut("{id:int}")]
    [RequirePermission(PermissionCodes.AccountsUpdate)]
    public async Task<ActionResult<AccountDto>> Update(int id, [FromBody] UpdateAccountRequest request)
    {
        var item = await service.UpdateAsync(id, request, CurrentUserId);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PermissionCodes.AccountsDelete)]
    public async Task<IActionResult> Delete(int id)
        => await service.DeleteAsync(id, CurrentUserId) ? NoContent() : NotFound();
}
