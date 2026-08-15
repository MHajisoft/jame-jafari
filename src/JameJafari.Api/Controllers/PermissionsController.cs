using JameJafari.Core.DTOs;
using JameJafari.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JameJafari.Api.Controllers;

[Authorize]
[Route("api/permissions")]
public class PermissionsController(PermissionService permissions) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PermissionResponse>>> GetAll()
        => Ok(await permissions.GetAllAsync());
}
