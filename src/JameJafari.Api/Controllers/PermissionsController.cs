using JameJafari.Core.DTOs;
using JameJafari.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JameJafari.Api.Controllers;

[Authorize]
[Route("api/permissions")]
public class PermissionsController(AppDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PermissionDto>>> GetAll()
    {
        var items = await db.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Code)
            .Select(p => new PermissionDto(p.Id, p.Code, p.Name, p.Description, p.Code.Split('.')[0]))
            .ToListAsync();
        return Ok(items);
    }
}
