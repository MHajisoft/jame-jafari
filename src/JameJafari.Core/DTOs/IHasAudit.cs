namespace JameJafari.Core.DTOs;

/// <summary>API response that carries permission-gated audit metadata.</summary>
public interface IHasAudit
{
    AuditInfoResponse Audit { get; set; }
}
