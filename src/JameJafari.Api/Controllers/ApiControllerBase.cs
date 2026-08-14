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

    protected static readonly AuditInfoDto NoAudit = new(default, null, null, null);

    protected T ApplyIfDenied<T>(T value, string permission, Func<T, T> strip) =>
        HasPermission(permission) ? value : strip(value);

    protected IReadOnlyList<T> ApplyIfDenied<T>(
        IReadOnlyList<T> items,
        string permission,
        Func<T, T> strip) =>
        HasPermission(permission) ? items : items.Select(strip).ToList();

    protected PagedResult<T> ApplyIfDenied<T>(
        PagedResult<T> page,
        string permission,
        Func<T, T> strip) =>
        HasPermission(permission)
            ? page
            : page with { Items = page.Items.Select(strip).ToList() };

    protected T ApplyAuditVisibility<T>(T dto, Func<T, T> strip) =>
        ApplyIfDenied(dto, PermissionCodes.AuditView, strip);

    protected IReadOnlyList<T> ApplyAuditVisibility<T>(IReadOnlyList<T> items, Func<T, T> strip) =>
        ApplyIfDenied(items, PermissionCodes.AuditView, strip);

    protected PagedResult<T> ApplyAuditVisibility<T>(PagedResult<T> page, Func<T, T> strip) =>
        ApplyIfDenied(page, PermissionCodes.AuditView, strip);

    protected IncomeTransactionDto ApplyIncomeVisibility(IncomeTransactionDto dto) =>
        ApplyAuditVisibility(
            ApplyIfDenied(dto, PermissionCodes.AttachmentsView, static d => d with { Attachments = NoAttachments }),
            static d => d with { Audit = NoAudit });

    protected CostTransactionDto ApplyCostVisibility(CostTransactionDto dto) =>
        ApplyAuditVisibility(
            ApplyIfDenied(dto, PermissionCodes.AttachmentsView, static d => d with { Attachments = NoAttachments }),
            static d => d with { Audit = NoAudit });

    protected PagedResult<IncomeTransactionDto> ApplyIncomeVisibility(PagedResult<IncomeTransactionDto> page) =>
        page with { Items = page.Items.Select(ApplyIncomeVisibility).ToList() };

    protected PagedResult<CostTransactionDto> ApplyCostVisibility(PagedResult<CostTransactionDto> page) =>
        page with { Items = page.Items.Select(ApplyCostVisibility).ToList() };

    /// <summary>Returns null when OK; otherwise a BadRequest result.</summary>
    protected ActionResult? EnsureCanAddAttachments(IFormFileCollection? documents)
    {
        if (documents is null || documents.Count == 0) return null;
        if (HasPermission(PermissionCodes.AttachmentsAdd)) return null;
        return BadRequest(new { message = "مجوز افزودن پیوست ندارید" });
    }
}
