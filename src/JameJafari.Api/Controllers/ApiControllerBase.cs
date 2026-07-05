using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace JameJafari.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    protected bool HasPermission(string permission) =>
        User.HasClaim("permission", permission);
}
