namespace JameJafari.Infrastructure.Messaging;

public sealed class MessengerSendResult
{
    public IReadOnlyList<string> MessageIds { get; init; } = [];
    public string? ChatId { get; init; }
    public string? MessageId => MessageIds.Count > 0 ? MessageIds[0] : null;
}

/// <summary>Lightweight reachability probe (e.g. getMe) so ISP-filtered messengers can be skipped.</summary>
public sealed class MessengerHealthResult
{
    public static readonly TimeSpan DefaultProbeTimeout = TimeSpan.FromSeconds(5);

    public bool IsAvailable { get; init; }
    public string? ErrorMessage { get; init; }

    public static MessengerHealthResult Available() => new() { IsAvailable = true };

    public static MessengerHealthResult Unavailable(string message) =>
        new() { IsAvailable = false, ErrorMessage = message };

    /// <summary>Runs <paramref name="probe"/> with a short timeout; returns Unavailable on network/filter failure.</summary>
    public static async Task<MessengerHealthResult> ProbeAsync(
        string unavailableMessage,
        Func<CancellationToken, Task> probe,
        CancellationToken cancellationToken = default,
        TimeSpan? timeout = null)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout ?? DefaultProbeTimeout);
            await probe(cts.Token);
            return Available();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Unavailable(unavailableMessage);
        }
    }
}
