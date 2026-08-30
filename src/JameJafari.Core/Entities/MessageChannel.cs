using JameJafari.Core.Enums;

namespace JameJafari.Core.Entities;

public class MessageChannel : AuditableEntity
{
    public string Name { get; set; } = "";
    public MessengerKind MessengerKind { get; set; }
    public string ExternalChatId { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
