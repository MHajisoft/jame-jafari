using System.Security.Claims;
using JameJafari.Core.Constants;
using JameJafari.Core.DTOs;
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

    protected static readonly IReadOnlyList<TransactionAttachmentDto> NoAttachments =
        Array.Empty<TransactionAttachmentDto>();

    protected IncomeTransactionDto ApplyAttachmentVisibility(IncomeTransactionDto dto) =>
        HasPermission(PermissionCodes.AttachmentsView) ? dto : dto with { Attachments = NoAttachments };

    protected CostTransactionDto ApplyAttachmentVisibility(CostTransactionDto dto) =>
        HasPermission(PermissionCodes.AttachmentsView) ? dto : dto with { Attachments = NoAttachments };

    protected PagedResult<IncomeTransactionDto> ApplyAttachmentVisibility(PagedResult<IncomeTransactionDto> page) =>
        HasPermission(PermissionCodes.AttachmentsView)
            ? page
            : page with { Items = page.Items.Select(ApplyAttachmentVisibility).ToList() };

    protected PagedResult<CostTransactionDto> ApplyAttachmentVisibility(PagedResult<CostTransactionDto> page) =>
        HasPermission(PermissionCodes.AttachmentsView)
            ? page
            : page with { Items = page.Items.Select(ApplyAttachmentVisibility).ToList() };

    /// <summary>Returns null when OK; otherwise a BadRequest result.</summary>
    protected ActionResult? EnsureCanAddAttachments(IFormFileCollection? documents)
    {
        if (documents is null || documents.Count == 0) return null;
        if (HasPermission(PermissionCodes.AttachmentsAdd)) return null;
        return BadRequest(new { message = "مجوز افزودن پیوست ندارید" });
    }
}
