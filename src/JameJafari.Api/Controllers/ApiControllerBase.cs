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

    protected AccountDto ApplyAuditVisibility(AccountDto dto) =>
        HasPermission(PermissionCodes.AuditView) ? dto : dto with { Audit = NoAudit };

    protected IReadOnlyList<AccountDto> ApplyAuditVisibility(IReadOnlyList<AccountDto> items) =>
        HasPermission(PermissionCodes.AuditView) ? items : items.Select(ApplyAuditVisibility).ToList();

    protected PersonDto ApplyAuditVisibility(PersonDto dto) =>
        HasPermission(PermissionCodes.AuditView) ? dto : dto with { Audit = NoAudit };

    protected PagedResult<PersonDto> ApplyAuditVisibility(PagedResult<PersonDto> page) =>
        HasPermission(PermissionCodes.AuditView)
            ? page
            : page with { Items = page.Items.Select(ApplyAuditVisibility).ToList() };

    protected UserDto ApplyAuditVisibility(UserDto dto) =>
        HasPermission(PermissionCodes.AuditView) ? dto : dto with { Audit = NoAudit };

    protected PagedResult<UserDto> ApplyAuditVisibility(PagedResult<UserDto> page) =>
        HasPermission(PermissionCodes.AuditView)
            ? page
            : page with { Items = page.Items.Select(ApplyAuditVisibility).ToList() };

    protected CostTypeDto ApplyAuditVisibility(CostTypeDto dto) =>
        HasPermission(PermissionCodes.AuditView) ? dto : dto with { Audit = NoAudit };

    protected IReadOnlyList<CostTypeDto> ApplyAuditVisibility(IReadOnlyList<CostTypeDto> items) =>
        HasPermission(PermissionCodes.AuditView) ? items : items.Select(ApplyAuditVisibility).ToList();

    protected FoodGenerationDto ApplyAuditVisibility(FoodGenerationDto dto) =>
        HasPermission(PermissionCodes.AuditView) ? dto : dto with { Audit = NoAudit };

    protected IReadOnlyList<FoodGenerationDto> ApplyAuditVisibility(IReadOnlyList<FoodGenerationDto> items) =>
        HasPermission(PermissionCodes.AuditView) ? items : items.Select(ApplyAuditVisibility).ToList();

    protected IncomeTransactionDto ApplyAuditVisibility(IncomeTransactionDto dto) =>
        HasPermission(PermissionCodes.AuditView) ? dto : dto with { Audit = NoAudit };

    protected CostTransactionDto ApplyAuditVisibility(CostTransactionDto dto) =>
        HasPermission(PermissionCodes.AuditView) ? dto : dto with { Audit = NoAudit };

    protected IncomeTransactionDto ApplyIncomeVisibility(IncomeTransactionDto dto) =>
        ApplyAuditVisibility(ApplyAttachmentVisibility(dto));

    protected CostTransactionDto ApplyCostVisibility(CostTransactionDto dto) =>
        ApplyAuditVisibility(ApplyAttachmentVisibility(dto));

    protected IncomeTransactionDto ApplyAttachmentVisibility(IncomeTransactionDto dto) =>
        HasPermission(PermissionCodes.AttachmentsView) ? dto : dto with { Attachments = NoAttachments };

    protected CostTransactionDto ApplyAttachmentVisibility(CostTransactionDto dto) =>
        HasPermission(PermissionCodes.AttachmentsView) ? dto : dto with { Attachments = NoAttachments };

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
