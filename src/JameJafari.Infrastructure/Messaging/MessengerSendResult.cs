using JameJafari.Infrastructure.Bale;

namespace JameJafari.Infrastructure.Messaging;

public sealed class MessengerSendResult
{
    public IReadOnlyList<int> MessageIds { get; init; } = [];
    public BaleChatResult? Chat { get; init; }
    public int MessageId => MessageIds.Count > 0 ? MessageIds[0] : 0;
}
