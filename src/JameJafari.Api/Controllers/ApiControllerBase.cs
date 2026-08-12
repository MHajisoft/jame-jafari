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

    /// <summary>Null = all records (has view). Otherwise restrict to current user's creations.</summary>
    protected int? OwnRecordsFilter(string viewPermission) =>
        HasPermission(viewPermission) ? null : CurrentUserId;
}
