using JameJafari.Core.Helpers;

namespace JameJafari.Core.DTOs;

/// <summary>
/// Base for auditable API responses: shared <see cref="Audit"/>, deep-clone, and fluent strip helpers.
/// </summary>
public abstract class ResponseBase : IHasAudit
{
    public AuditInfoResponse Audit { get; set; } = AuditInfoResponse.Empty;

    /// <summary>
    /// Deep copy via compiled expression trees — replace gated fields on the clone;
    /// never mutate the cached original.
    /// </summary>
    public T CloneResponse<T>() where T : ResponseBase => ExpressionDeepClone.Clone((T)this);

    /// <summary>Deep-clone and clear audit (safe for FusionCache).</summary>
    public T WithoutAudit<T>() where T : ResponseBase
    {
        var clone = CloneResponse<T>();
        clone.Audit = AuditInfoResponse.Empty;
        return clone;
    }
}
