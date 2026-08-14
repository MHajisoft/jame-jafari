using System.Security.Claims;
using JameJafari.Core.Constants;
using Microsoft.AspNetCore.Mvc;

namespace JameJafari.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected int CurrentUserId
    {
        get
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (raw is null || !int.TryParse(raw, out var id))
                throw new UnauthorizedAccessException();
            return id;
        }
    }

    protected bool HasPermission(string permission) =>
        User.HasClaim("permission", permission);

    /// <summary>Null = all records (has view). Otherwise restrict to current user's creations.</summary>
    protected int? OwnRecordsFilter(string viewPermission) =>
        HasPermission(viewPermission) ? null : CurrentUserId;

    /// <summary>Returns null when OK; otherwise a BadRequest result.</summary>
    protected ActionResult? EnsureCanAddAttachments(IFormFileCollection? documents)
    {
        if (documents is null || documents.Count == 0) return null;
        if (HasPermission(PermissionCodes.AttachmentsAdd)) return null;
        return BadRequest(new { message = "مجوز افزودن پیوست ندارید" });
    }
}
