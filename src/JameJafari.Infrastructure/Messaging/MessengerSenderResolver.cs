using JameJafari.Core.Enums;

namespace JameJafari.Infrastructure.Messaging;

public class MessengerSenderResolver(IEnumerable<IMessengerSender> senders)
{
    private readonly IReadOnlyDictionary<MessengerKind, IMessengerSender> _senders =
        senders.ToDictionary(s => s.Kind);

    public IMessengerSender Get(MessengerKind kind) =>
        _senders.TryGetValue(kind, out var sender)
            ? sender
            : throw new InvalidOperationException("پیام‌رسان پشتیبانی نمی‌شود");

    public IReadOnlyList<IMessengerSender> GetAll() => _senders.Values.ToList();
}
