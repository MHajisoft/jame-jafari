namespace JameJafari.Core.DTOs;

/// <summary>
/// Auditable response that also carries permission-gated attachments (e.g. transactions).
/// </summary>
public abstract class AttachmentResponseBase : ResponseBase, IHasAttachments
{
    public IReadOnlyList<TransactionAttachmentResponse> Attachments { get; set; } = [];

    /// <summary>Deep-clone and clear attachments (safe for FusionCache).</summary>
    public T WithoutAttachments<T>() where T : AttachmentResponseBase
    {
        var clone = CloneResponse<T>();
        clone.Attachments = Array.Empty<TransactionAttachmentResponse>();
        return clone;
    }

    /// <summary>
    /// One deep-clone, then clear audit and/or attachments based on caller permissions.
    /// Returns <c>this</c> unchanged when nothing needs stripping.
    /// </summary>
    public T ApplyVisibility<T>(bool canViewAudit, bool canViewAttachments) where T : AttachmentResponseBase
    {
        if (canViewAudit && canViewAttachments)
            return (T)this;

        var clone = CloneResponse<T>();
        if (!canViewAudit)
            clone.Audit = AuditInfoResponse.Empty;
        if (!canViewAttachments)
            clone.Attachments = Array.Empty<TransactionAttachmentResponse>();
        return clone;
    }
}
