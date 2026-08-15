using System.Security.Claims;
using JameJafari.Core.Constants;
using JameJafari.Core.DTOs;

namespace JameJafari.Api.Services;

/// <summary>
/// Strips permission-gated fields from API responses (stateless).
/// Generics constrain on <see cref="ResponseBase"/> / <see cref="AttachmentResponseBase"/> — fluent strip, never mutates cache.
/// </summary>
public static class ResponseVisibility
{
    public static T Apply<T>(T item, ClaimsPrincipal user)
        where T : ResponseBase =>
        CanViewAudit(user) ? item : item.WithoutAudit<T>();

    public static IReadOnlyList<T> Apply<T>(IReadOnlyList<T> items, ClaimsPrincipal user)
        where T : ResponseBase =>
        CanViewAudit(user)
            ? items
            : [.. items.Select(i => i.WithoutAudit<T>())];

    public static PagedResult<T> Apply<T>(PagedResult<T> page, ClaimsPrincipal user)
        where T : ResponseBase =>
        CanViewAudit(user)
            ? page
            : page with { Items = [.. page.Items.Select(i => i.WithoutAudit<T>())] };

    public static T ApplyAttachments<T>(T item, ClaimsPrincipal user)
        where T : AttachmentResponseBase =>
        item.ApplyVisibility<T>(CanViewAudit(user), CanViewAttachments(user));

    public static PagedResult<T> ApplyAttachments<T>(PagedResult<T> page, ClaimsPrincipal user)
        where T : AttachmentResponseBase
    {
        if (CanViewAudit(user) && CanViewAttachments(user))
            return page;

        return page with { Items = [.. page.Items.Select(i => ApplyAttachments(i, user))] };
    }

    private static bool CanViewAudit(ClaimsPrincipal user) =>
        user.HasClaim("permission", PermissionCodes.AuditView);

    private static bool CanViewAttachments(ClaimsPrincipal user) =>
        user.HasClaim("permission", PermissionCodes.AttachmentsView);
}
