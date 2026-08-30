using JameJafari.Core.Enums;

namespace JameJafari.Core.Entities;

public class BaleMessage : AuditableEntity
{
    public MessengerKind MessengerKind { get; set; } = MessengerKind.Bale;
    public BaleMessageType MessageType { get; set; }
    public BaleMessageTargetKind TargetKind { get; set; }
    public int? MessageChannelId { get; set; }
    public MessageChannel? MessageChannel { get; set; }
    public int? PersonGroupId { get; set; }
    public PersonGroup? PersonGroup { get; set; }
    public Guid? BroadcastBatchId { get; set; }
    public string ChatId { get; set; } = "";
    public string? TargetMobile { get; set; }
    public int? BaleMessageId { get; set; }
    public bool DeletedFromBale { get; set; }
    public string? Text { get; set; }
    public string? Caption { get; set; }
    public string? LinkUrl { get; set; }
    public string? LinkLabel { get; set; }
    public List<string> AttachmentPaths { get; set; } = [];
    public List<int> BaleMessageIds { get; set; } = [];
    public BaleMessageStatus Status { get; set; } = BaleMessageStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }
    public int? IncomeTransactionId { get; set; }
    public IncomeTransaction? IncomeTransaction { get; set; }
    public int? PersonId { get; set; }
    public Person? Person { get; set; }
}
