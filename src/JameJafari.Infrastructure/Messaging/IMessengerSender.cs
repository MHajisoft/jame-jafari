using JameJafari.Core.Enums;
using JameJafari.Infrastructure.Bale;

namespace JameJafari.Infrastructure.Messaging;

public sealed class MessengerSendRequest
{
    public BaleMessageType MessageType { get; init; }
    public string ChatId { get; init; } = "";
    public string? Text { get; init; }
    public string? Caption { get; init; }
    public IReadOnlyList<string> AttachmentPaths { get; init; } = [];
}

public sealed class MessengerEditRequest
{
    public BaleMessageType MessageType { get; init; }
    public string ChatId { get; init; } = "";
    public int MessageId { get; init; }
    public string? Text { get; init; }
    public string? Caption { get; init; }
}

public sealed class MessengerDeleteRequest
{
    public string ChatId { get; init; } = "";
    public int MessageId { get; init; }
}

public interface IMessengerSender
{
    MessengerKind Kind { get; }
    bool IsConfigured { get; }
    Task<MessengerSendResult> SendAsync(MessengerSendRequest request, CancellationToken cancellationToken = default);
    Task EditAsync(MessengerEditRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(MessengerDeleteRequest request, CancellationToken cancellationToken = default);
}
