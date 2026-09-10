namespace JameJafari.Infrastructure.Messaging;

public sealed class MessengerSendResult
{
    public IReadOnlyList<string> MessageIds { get; init; } = [];
    public string? ChatId { get; init; }
    public string? MessageId => MessageIds.Count > 0 ? MessageIds[0] : null;
}
