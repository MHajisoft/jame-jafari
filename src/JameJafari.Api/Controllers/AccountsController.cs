using JameJafari.Api.Authorization;
using JameJafari.Api.Services;
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
    public async Task<ActionResult<IReadOnlyList<AccountResponse>>> GetAll([FromQuery] bool activeOnly = true)
        => Ok(ResponseVisibility.Apply(await service.GetAllAsync(activeOnly), User));

    [HttpGet("{id:int}")]
    [RequirePermission(PermissionCodes.AccountsView)]
    public async Task<ActionResult<AccountResponse>> GetById(int id)
    {
        var item = await service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(ResponseVisibility.Apply(item, User));
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.AccountsCreate)]
    public async Task<ActionResult<AccountResponse>> Create([FromBody] CreateAccountRequest request)
        => Ok(ResponseVisibility.Apply(await service.CreateAsync(request, CurrentUserId), User));

    [HttpPut("{id:int}")]
    [RequirePermission(PermissionCodes.AccountsUpdate)]
    public async Task<ActionResult<AccountResponse>> Update(int id, [FromBody] UpdateAccountRequest request)
    {
        var item = await service.UpdateAsync(id, request, CurrentUserId);
        return item is null ? NotFound() : Ok(ResponseVisibility.Apply(item, User));
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PermissionCodes.AccountsDelete)]
    public async Task<IActionResult> Delete(int id)
        => await service.DeleteAsync(id, CurrentUserId) ? NoContent() : NotFound();
}
