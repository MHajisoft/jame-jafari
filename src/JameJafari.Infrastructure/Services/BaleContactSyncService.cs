using JameJafari.Core.Entities;
using JameJafari.Core.Helpers;
using JameJafari.Core.Options;
using JameJafari.Infrastructure.Bale;
using JameJafari.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JameJafari.Infrastructure.Services;

public class BaleContactSyncService(
    AppDbContext db,
    BaleBotClient bale,
    BaleContactResolver resolver,
    IOptions<BaleOptions> options,
    ILogger<BaleContactSyncService> logger)
{
    const string ContactRequestText = "جهت دریافت پیام، لطفاً شماره تماس خود را با دکمه زیر به اشتراک بگذارید.";

    private readonly BaleOptions _options = options.Value;

    public async Task<int> ProcessWebhookUpdateAsync(BaleUpdateModels.BaleWebhookUpdate update, CancellationToken cancellationToken = default)
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
                var pending = await db.BaleContactLinks.AsNoTracking()
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

    public async Task<int> SyncFromUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var state = await GetOrCreateStateAsync(cancellationToken);

        if (state.LastUpdateId == 0)
        {
            var flushed = await bale.GetUpdatesAsync(offset: -1, cancellationToken: cancellationToken);
            if (flushed.Count > 0)
            {
                state.LastUpdateId = flushed.Max(u => u.UpdateId);
                await db.SaveChangesAsync(cancellationToken);
            }

            return 0;
        }

        var updates = await bale.GetUpdatesAsync(offset: state.LastUpdateId + 1, cancellationToken: cancellationToken);
        var linked = 0;
        foreach (var update in updates)
            linked += await ProcessWebhookUpdateAsync(update, cancellationToken);

        return linked;
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
            person.BaleChatId = null;
        else if (newPhone != oldPhone)
            person.BaleChatId = null;

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
            logger.LogWarning(ex, "Bale contact reconcile failed for person {PersonId}", person.Id);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception saveEx)
            {
                logger.LogWarning(saveEx, "Failed to save person BaleChatId after reconcile error for person {PersonId}", person.Id);
            }
        }
    }

    async Task ApplyLocalLinkAsync(Person person, string? newPhone, CancellationToken cancellationToken)
    {
        if (newPhone is null)
            return;

        var link = await db.BaleContactLinks.FirstOrDefaultAsync(l => l.NormalizedPhone == newPhone, cancellationToken);
        if (link is null)
            return;

        person.BaleChatId = link.ChatId;
        link.PersonId = person.Id;
        link.LinkedAt = DateTime.UtcNow;
    }

    async Task DisassociateOldLinkAsync(int personId, string oldPhone, CancellationToken cancellationToken)
    {
        var oldLink = await db.BaleContactLinks.FirstOrDefaultAsync(
            l => l.NormalizedPhone == oldPhone && l.PersonId == personId, cancellationToken);
        if (oldLink is not null)
            oldLink.PersonId = null;
    }

    async Task TrySendContactRequestAsync(long chatId, CancellationToken cancellationToken)
    {
        try
        {
            await bale.SendContactRequestAsync(chatId, ContactRequestText, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send Bale contact request to chat {ChatId}", chatId);
        }
    }

    async Task<BaleBotState> GetOrCreateStateAsync(CancellationToken cancellationToken)
    {
        var state = await db.BaleBotStates.OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken);
        if (state is not null)
            return state;

        state = new BaleBotState();
        db.BaleBotStates.Add(state);
        await db.SaveChangesAsync(cancellationToken);
        return state;
    }

    async Task AdvanceUpdateIdAsync(BaleBotState state, int updateId, CancellationToken cancellationToken)
    {
        if (updateId <= state.LastUpdateId)
            return;

        state.LastUpdateId = updateId;
        await db.SaveChangesAsync(cancellationToken);
    }

    static long? ResolveChatId(BaleUpdateModels.BaleWebhookMessage? message)
    {
        if (message is null || !IsPrivate(message))
            return null;

        return message.Chat?.Id ?? message.From?.Id ?? message.Contact?.UserId;
    }

    static bool IsPrivate(BaleUpdateModels.BaleWebhookMessage message)
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
