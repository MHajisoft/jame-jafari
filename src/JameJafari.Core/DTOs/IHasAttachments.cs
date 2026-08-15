namespace JameJafari.Core.DTOs;

/// <summary>API response that carries permission-gated attachments.</summary>
public interface IHasAttachments
{
    IReadOnlyList<TransactionAttachmentResponse> Attachments { get; set; }
}
