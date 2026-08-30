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
[Route("api/person-groups")]
public class PersonGroupsController(PersonGroupService service) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.PersonGroupsView)]
    public async Task<ActionResult<PagedResult<PersonGroupResponse>>> GetAll(
        [FromQuery] bool activeOnly = true,
        [FromQuery, Range(1, 100)] int page = 1,
        [FromQuery, Range(1, 200)] int pageSize = 20)
        => Ok(ResponseVisibility.Apply(await service.GetPagedAsync(activeOnly, page, pageSize), User));

    [HttpGet("{id:int}")]
    [RequirePermission(PermissionCodes.PersonGroupsView)]
    public async Task<ActionResult<PersonGroupResponse>> GetById(int id)
    {
        var item = await service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(ResponseVisibility.Apply(item, User));
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.PersonGroupsCreate)]
    public async Task<ActionResult<PersonGroupResponse>> Create([FromBody] CreatePersonGroupRequest request)
    {
        try
        {
            return Ok(ResponseVisibility.Apply(await service.CreateAsync(request, CurrentUserId), User));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [RequirePermission(PermissionCodes.PersonGroupsUpdate)]
    public async Task<ActionResult<PersonGroupResponse>> Update(int id, [FromBody] UpdatePersonGroupRequest request)
    {
        try
        {
            var item = await service.UpdateAsync(id, request, CurrentUserId);
            return item is null ? NotFound() : Ok(ResponseVisibility.Apply(item, User));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PermissionCodes.PersonGroupsDelete)]
    public async Task<IActionResult> Delete(int id)
        => await service.DeleteAsync(id, CurrentUserId) ? NoContent() : NotFound();
}
