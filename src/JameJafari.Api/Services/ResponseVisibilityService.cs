using System.Security.Claims;
using JameJafari.Core.Constants;
using JameJafari.Core.DTOs;

namespace JameJafari.Api.Services;

/// <summary>Strips permission-gated fields from API responses (audit, attachments).</summary>
public class ResponseVisibilityService
{
    private static readonly IReadOnlyList<TransactionAttachmentDto> NoAttachments =
        Array.Empty<TransactionAttachmentDto>();

    private static readonly AuditInfoDto NoAudit = new(default, null, null, null);

    public AccountDto ForResponse(AccountDto dto, ClaimsPrincipal user) =>
        HasPermission(user, PermissionCodes.AuditView) ? dto : dto with { Audit = NoAudit };

    public IReadOnlyList<AccountDto> ForResponse(IReadOnlyList<AccountDto> items, ClaimsPrincipal user) =>
        HasPermission(user, PermissionCodes.AuditView)
            ? items
            : items.Select(d => d with { Audit = NoAudit }).ToList();

    public PersonDto ForResponse(PersonDto dto, ClaimsPrincipal user) =>
        HasPermission(user, PermissionCodes.AuditView) ? dto : dto with { Audit = NoAudit };

    public PagedResult<PersonDto> ForResponse(PagedResult<PersonDto> page, ClaimsPrincipal user) =>
        HasPermission(user, PermissionCodes.AuditView)
            ? page
            : page with { Items = page.Items.Select(d => d with { Audit = NoAudit }).ToList() };

    public UserDto ForResponse(UserDto dto, ClaimsPrincipal user) =>
        HasPermission(user, PermissionCodes.AuditView) ? dto : dto with { Audit = NoAudit };

    public PagedResult<UserDto> ForResponse(PagedResult<UserDto> page, ClaimsPrincipal user) =>
        HasPermission(user, PermissionCodes.AuditView)
            ? page
            : page with { Items = page.Items.Select(d => d with { Audit = NoAudit }).ToList() };

    public CostTypeDto ForResponse(CostTypeDto dto, ClaimsPrincipal user) =>
        HasPermission(user, PermissionCodes.AuditView) ? dto : dto with { Audit = NoAudit };

    public IReadOnlyList<CostTypeDto> ForResponse(IReadOnlyList<CostTypeDto> items, ClaimsPrincipal user) =>
        HasPermission(user, PermissionCodes.AuditView)
            ? items
            : items.Select(d => d with { Audit = NoAudit }).ToList();

    public FoodGenerationDto ForResponse(FoodGenerationDto dto, ClaimsPrincipal user) =>
        HasPermission(user, PermissionCodes.AuditView) ? dto : dto with { Audit = NoAudit };

    public IReadOnlyList<FoodGenerationDto> ForResponse(IReadOnlyList<FoodGenerationDto> items, ClaimsPrincipal user) =>
        HasPermission(user, PermissionCodes.AuditView)
            ? items
            : items.Select(d => d with { Audit = NoAudit }).ToList();

    public IncomeTransactionDto ForIncomeResponse(IncomeTransactionDto dto, ClaimsPrincipal user)
    {
        if (HasPermission(user, PermissionCodes.AttachmentsView) && HasPermission(user, PermissionCodes.AuditView))
            return dto;

        var shaped = HasPermission(user, PermissionCodes.AttachmentsView)
            ? dto
            : dto with { Attachments = NoAttachments };

        return HasPermission(user, PermissionCodes.AuditView)
            ? shaped
            : shaped with { Audit = NoAudit };
    }

    public PagedResult<IncomeTransactionDto> ForIncomeResponse(PagedResult<IncomeTransactionDto> page, ClaimsPrincipal user)
    {
        if (HasPermission(user, PermissionCodes.AttachmentsView) && HasPermission(user, PermissionCodes.AuditView))
            return page;

        return page with { Items = page.Items.Select(d => ForIncomeResponse(d, user)).ToList() };
    }

    public CostTransactionDto ForCostResponse(CostTransactionDto dto, ClaimsPrincipal user)
    {
        if (HasPermission(user, PermissionCodes.AttachmentsView) && HasPermission(user, PermissionCodes.AuditView))
            return dto;

        var shaped = HasPermission(user, PermissionCodes.AttachmentsView)
            ? dto
            : dto with { Attachments = NoAttachments };

        return HasPermission(user, PermissionCodes.AuditView)
            ? shaped
            : shaped with { Audit = NoAudit };
    }

    public PagedResult<CostTransactionDto> ForCostResponse(PagedResult<CostTransactionDto> page, ClaimsPrincipal user)
    {
        if (HasPermission(user, PermissionCodes.AttachmentsView) && HasPermission(user, PermissionCodes.AuditView))
            return page;

        return page with { Items = page.Items.Select(d => ForCostResponse(d, user)).ToList() };
    }

    private static bool HasPermission(ClaimsPrincipal user, string permission) =>
        user.HasClaim("permission", permission);
}
