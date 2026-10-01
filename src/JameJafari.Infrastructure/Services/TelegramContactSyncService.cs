using JameJafari.Core.Entities;
using JameJafari.Core.Enums;
using JameJafari.Core.Helpers;
using JameJafari.Core.Options;
using JameJafari.Infrastructure.Telegram;
using JameJafari.Infrastructure.Data;
using JameJafari.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JameJafari.Infrastructure.Services;

public class TelegramContactSyncService(
    AppDbContext db,
    TelegramBotClient bot,
    TelegramContactResolver resolver,
    MessengerSenderResolver senderResolver,
    IOptions<TelegramOptions> options,
    ILogger<TelegramContactSyncService> logger)
{
    const string ContactRequestText = "جهت دریافت پیام، لطفاً شماره تماس خود را با دکمه زیر به اشتراک بگذارید.";

    private readonly TelegramOptions _options = options.Value;

    public async Task<int> ProcessWebhookUpdateAsync(TelegramUpdateModels.TelegramWebhookUpdate update, CancellationToken cancellationToken = default)
    {
        var state = await GetOrCreateStateAsync(cancellationToken);
        if (update.UpdateId <= state.LastUpdateId)
            return 0;

        var message = update.ResolveMessage();
        var chatId = ResolveChatId(message);
        if (chatId is null)
        {
            await AdvanceUpdateIdAsync(state, update.UpdateId, cancellationToken);
            return 0;
        }

        var linked = 0;
        if (message!.Contact?.PhoneNumber is not null)
        {
            var personId = ParsePersonIdFromStart(message.Text);
            if (personId is null)
            {
                var pending = await db.TelegramContactLinks.AsNoTracking()
                    .FirstOrDefaultAsync(l => l.ChatId == chatId && l.PersonId != null, cancellationToken);
                personId = pending?.PersonId;
            }

            await resolver.LinkByPhoneAsync(message.Contact.PhoneNumber, chatId.Value, personId, cancellationToken);
            linked = 1;
        }
        else if (message.Text?.StartsWith("/start", StringComparison.OrdinalIgnoreCase) == true)
        {
            var personId = ParsePersonIdFromStart(message.Text);
            await resolver.LinkByChatAsync(chatId.Value, personId, cancellationToken);
            await TrySendContactRequestAsync(chatId.Value, cancellationToken);
            linked = 1;
        }

        await AdvanceUpdateIdAsync(state, update.UpdateId, cancellationToken);
        return linked;
    }

    public async Task<(int Linked, string? Error)> SyncFromUpdatesAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
            return (0, "توکن تلگرام تنظیم نشده است");

        var health = await senderResolver.Get(MessengerKind.Telegram).CheckHealthAsync(cancellationToken);
        if (!health.IsAvailable)
        {
            logger.LogWarning("Telegram contact sync skipped: {Error}", health.ErrorMessage);
            return (0, health.ErrorMessage);
        }

        try
        {
            var state = await GetOrCreateStateAsync(cancellationToken);

            // Cold start: process pending updates (incl. /start + contact) instead of discarding via offset=-1.
            int? offset = state.LastUpdateId > 0 ? state.LastUpdateId + 1 : null;
            var updates = await bot.GetUpdatesAsync(offset: offset, limit: 100, cancellationToken: cancellationToken);
            var linked = 0;
            foreach (var update in updates)
                linked += await ProcessWebhookUpdateAsync(update, cancellationToken);

            return (linked, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Telegram contact sync failed");
            return (0, ex.Message);
        }
    }

    public async Task<long?> ResolveChatIdWithSyncAsync(Person person, CancellationToken cancellationToken = default)
    {
        var chatId = await resolver.ResolveChatIdAsync(person, cancellationToken);
        if (chatId is not null || !_options.IsConfigured)
            return chatId;

        await SyncFromUpdatesAsync(cancellationToken);
        return await resolver.ResolveChatIdAsync(person, cancellationToken);
    }

    public async Task ReconcilePersonMobileAsync(Person person, string? previousMobile, CancellationToken cancellationToken = default)
    {
        var newPhone = PhoneNormalizeHelper.Normalize(person.Mobile);
        var oldPhone = PhoneNormalizeHelper.Normalize(previousMobile);
        if (newPhone == oldPhone)
            return;

        if (newPhone is null)
            person.TelegramChatId = null;
        else if (newPhone != oldPhone)
            person.TelegramChatId = null;

        if (oldPhone is not null && oldPhone != newPhone)
            await DisassociateOldLinkAsync(person.Id, oldPhone, cancellationToken);

        if (!_options.IsConfigured)
        {
            await ApplyLocalLinkAsync(person, newPhone, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        try
        {
            await SyncFromUpdatesAsync(cancellationToken);
            await ApplyLocalLinkAsync(person, newPhone, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Telegram contact reconcile failed for person {PersonId}", person.Id);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception saveEx)
            {
                logger.LogWarning(saveEx, "Failed to save person TelegramChatId after reconcile error for person {PersonId}", person.Id);
            }
        }
    }

    async Task ApplyLocalLinkAsync(Person person, string? newPhone, CancellationToken cancellationToken)
    {
        if (newPhone is null)
            return;

        var link = await db.TelegramContactLinks.FirstOrDefaultAsync(l => l.NormalizedPhone == newPhone, cancellationToken);
        if (link is null)
            return;

        person.TelegramChatId = link.ChatId;
        link.PersonId = person.Id;
        link.LinkedAt = DateTime.UtcNow;
    }

    async Task DisassociateOldLinkAsync(int personId, string oldPhone, CancellationToken cancellationToken)
    {
        var oldLink = await db.TelegramContactLinks.FirstOrDefaultAsync(
            l => l.NormalizedPhone == oldPhone && l.PersonId == personId, cancellationToken);
        if (oldLink is not null)
            oldLink.PersonId = null;
    }

    async Task TrySendContactRequestAsync(long chatId, CancellationToken cancellationToken)
    {
        try
        {
            await bot.SendContactRequestAsync(chatId, ContactRequestText, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send Telegram contact request to chat {ChatId}", chatId);
        }
    }

    async Task<TelegramBotState> GetOrCreateStateAsync(CancellationToken cancellationToken)
    {
        var state = await db.TelegramBotStates.OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken);
        if (state is not null)
            return state;

        state = new TelegramBotState();
        db.TelegramBotStates.Add(state);
        await db.SaveChangesAsync(cancellationToken);
        return state;
    }

    async Task AdvanceUpdateIdAsync(TelegramBotState state, int updateId, CancellationToken cancellationToken)
    {
        if (updateId <= state.LastUpdateId)
            return;

        state.LastUpdateId = updateId;
        await db.SaveChangesAsync(cancellationToken);
    }

    static long? ResolveChatId(TelegramUpdateModels.TelegramWebhookMessage? message)
    {
        if (message is null || !IsPrivate(message))
            return null;

        return message.Chat?.Id ?? message.From?.Id ?? message.Contact?.UserId;
    }

    static bool IsPrivate(TelegramUpdateModels.TelegramWebhookMessage message)
    {
        var type = message.Chat?.Type;
        return type is null or "private";
    }

    static int? ParsePersonIdFromStart(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var parts = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return null;

        var cmd = parts[0];
        var at = cmd.IndexOf('@');
        if (at > 0)
            cmd = cmd[..at];
        if (!cmd.Equals("/start", StringComparison.OrdinalIgnoreCase))
            return null;

        var payload = parts[1];
        if (!payload.StartsWith("person_", StringComparison.OrdinalIgnoreCase))
            return null;

        return int.TryParse(payload["person_".Length..], out var id) ? id : null;
    }
}
