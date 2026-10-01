using JameJafari.Core.Enums;

namespace JameJafari.Infrastructure.Messaging;

public sealed class MessengerSendRequest
{
    public MessengerMessageType MessageType { get; init; }
    public string ChatId { get; init; } = "";
    public string? Text { get; init; }
    public string? Caption { get; init; }
    public IReadOnlyList<string> AttachmentPaths { get; init; } = [];
}

public sealed class MessengerEditRequest
{
    public MessengerMessageType MessageType { get; init; }
    public string ChatId { get; init; } = "";
    public string MessageId { get; init; } = "";
    public string? Text { get; init; }
    public string? Caption { get; init; }
}

public sealed class MessengerDeleteRequest
{
    public string ChatId { get; init; } = "";
    public string MessageId { get; init; } = "";
}

public interface IMessengerSender
{
    MessengerKind Kind { get; }
    bool IsConfigured { get; }
    /// <summary>Probe API reachability; failures (timeout/filter) must not throw.</summary>
    Task<MessengerHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default);
    Task<MessengerSendResult> SendAsync(MessengerSendRequest request, CancellationToken cancellationToken = default);
    Task EditAsync(MessengerEditRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(MessengerDeleteRequest request, CancellationToken cancellationToken = default);
}
