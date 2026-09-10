using JameJafari.Core.Enums;

namespace JameJafari.Core.Entities;

public class MessengerMessage : AuditableEntity
{
    public MessengerKind MessengerKind { get; set; } = MessengerKind.Bale;
    public MessengerMessageType MessageType { get; set; }
    public MessengerMessageTargetKind TargetKind { get; set; }
    public int? MessageChannelId { get; set; }
    public MessageChannel? MessageChannel { get; set; }
    public int? PersonGroupId { get; set; }
    public PersonGroup? PersonGroup { get; set; }
    public Guid? BroadcastBatchId { get; set; }
    public string ChatId { get; set; } = "";
    public string? TargetMobile { get; set; }
    public string? RemoteMessageId { get; set; }
    public bool DeletedFromRemote { get; set; }
    public string? Text { get; set; }
    public string? Caption { get; set; }
    public string? LinkUrl { get; set; }
    public string? LinkLabel { get; set; }
    public List<string> AttachmentPaths { get; set; } = [];
    public List<string> RemoteMessageIds { get; set; } = [];
    public MessengerMessageStatus Status { get; set; } = MessengerMessageStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }
    public int? IncomeTransactionId { get; set; }
    public IncomeTransaction? IncomeTransaction { get; set; }
    public int? PersonId { get; set; }
    public Person? Person { get; set; }
}
