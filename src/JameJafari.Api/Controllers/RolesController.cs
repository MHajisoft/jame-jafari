using JameJafari.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JameJafari.Api.Controllers;

[Authorize]
[Route("api/roles")]
public class RolesController(AppDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<object>> GetAll()
    {
        var roles = await db.Roles.Where(r => !r.IsDeleted)
            .Select(r => new { r.Id, r.Name, r.Description })
            .ToListAsync();
        return Ok(roles);
    }
}
