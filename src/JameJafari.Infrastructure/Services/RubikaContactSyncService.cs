using System.Text.Json;
using JameJafari.Core.DTOs;
using JameJafari.Core.Entities;
using JameJafari.Core.Helpers;
using JameJafari.Core.Options;
using JameJafari.Infrastructure.Data;
using JameJafari.Infrastructure.Rubika;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JameJafari.Infrastructure.Services;

public class RubikaContactSyncService(
    AppDbContext db,
    RubikaBotClient rubika,
    RubikaContactResolver resolver,
    IOptions<RubikaOptions> options,
    ILogger<RubikaContactSyncService> logger)
{
    const string ContactRequestText = "جهت دریافت پیام، لطفاً شماره تماس خود را با دکمه زیر به اشتراک بگذارید.";
    const string SharePhoneButtonId = "share_phone";
    const int MaxKnownGroupChats = 50;

    static readonly JsonSerializerOptions KnownChatsJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RubikaOptions _options = options.Value;

    public async Task<int> ProcessUpdateAsync(RubikaUpdate update, CancellationToken cancellationToken = default)
    {
        var chatId = ResolveChatId(update);
        if (string.IsNullOrWhiteSpace(chatId))
            return 0;

        await RememberGroupChatFromUpdateAsync(update, cancellationToken);

        var message = update.NewMessage;
        var type = update.Type ?? "";
        var phone = ResolvePhone(message);

        if (!string.IsNullOrWhiteSpace(phone))
        {
            var pending = await db.RubikaContactLinks.AsNoTracking()
                .FirstOrDefaultAsync(l => l.ChatId == chatId && l.PersonId != null, cancellationToken);
            await resolver.LinkByPhoneAsync(phone, chatId, pending?.PersonId, cancellationToken);
            return 1;
        }

        if (type.Equals("StartedBot", StringComparison.OrdinalIgnoreCase)
            || message?.Text?.StartsWith("/start", StringComparison.OrdinalIgnoreCase) == true)
        {
            await resolver.LinkByChatAsync(chatId, null, cancellationToken);
            await TrySendContactRequestAsync(chatId, cancellationToken);
            return 1;
        }

        return 0;
    }

    public async Task<int> SyncFromUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var result = await ConsumeUpdatesAsync(cancellationToken);
        return result.LinkedContacts;
    }

    /// <summary>
    /// Rubika does not show group/channel chat_id in the client UI. Add the bot as admin, then send a
    /// message the bot can see (mention, /command, or BotFather «all messages»). Updates carry chat_id;
    /// getChat supplies title/type. Webhook and polling both remember Group/Channel chats.
    /// </summary>
    public async Task<IReadOnlyList<RubikaDiscoveredChatResponse>> DiscoverGroupChatsAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("توکن بازوی روبیکا تنظیم نشده است");

        var known = await LoadKnownGroupChatsAsync(cancellationToken);
        var map = known.ToDictionary(c => c.ChatId, StringComparer.Ordinal);

        var fresh = await ConsumeUpdatesAsync(cancellationToken);
        foreach (var chat in fresh.DiscoveredChats)
            map[chat.ChatId] = chat;

        await SaveKnownGroupChatsAsync(map.Values.ToList(), cancellationToken);
        return map.Values
            .OrderBy(c => c.Title ?? c.ChatId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    async Task<(int LinkedContacts, IReadOnlyList<RubikaDiscoveredChatResponse> DiscoveredChats)> ConsumeUpdatesAsync(
        CancellationToken cancellationToken)
    {
        var state = await GetOrCreateStateAsync(cancellationToken);
        var discovered = new Dictionary<string, RubikaDiscoveredChatResponse>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(state.LastOffsetId))
        {
            // Cold start: process /start + contact (and remember groups) instead of discard-only.
            var first = await rubika.GetUpdatesAsync(limit: 100, cancellationToken: cancellationToken);
            var firstLinked = 0;
            foreach (var update in first.Updates)
            {
                firstLinked += await ProcessUpdateAsync(update, cancellationToken);
                await TryResolveGroupChatAsync(ResolveChatId(update), discovered, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(first.NextOffsetId))
            {
                state.LastOffsetId = first.NextOffsetId;
                await db.SaveChangesAsync(cancellationToken);
            }

            foreach (var chat in await LoadKnownGroupChatsAsync(cancellationToken))
                discovered[chat.ChatId] = chat;

            return (firstLinked, discovered.Values.ToList());
        }

        var page = await rubika.GetUpdatesAsync(state.LastOffsetId, cancellationToken: cancellationToken);
        var linked = 0;
        foreach (var update in page.Updates)
        {
            linked += await ProcessUpdateAsync(update, cancellationToken);
            await TryResolveGroupChatAsync(ResolveChatId(update), discovered, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(page.NextOffsetId))
        {
            state.LastOffsetId = page.NextOffsetId;
            await db.SaveChangesAsync(cancellationToken);
        }

        foreach (var chat in await LoadKnownGroupChatsAsync(cancellationToken))
            discovered[chat.ChatId] = chat;

        return (linked, discovered.Values.ToList());
    }

    async Task RememberGroupChatFromUpdateAsync(RubikaUpdate update, CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, RubikaDiscoveredChatResponse>(StringComparer.Ordinal);
        await TryResolveGroupChatAsync(ResolveChatId(update), map, cancellationToken);
        if (map.Count == 0)
            return;

        var known = await LoadKnownGroupChatsAsync(cancellationToken);
        foreach (var chat in known)
            map.TryAdd(chat.ChatId, chat);

        await SaveKnownGroupChatsAsync(map.Values.ToList(), cancellationToken);
    }

    async Task TryResolveGroupChatAsync(
        string? chatId,
        Dictionary<string, RubikaDiscoveredChatResponse> discovered,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(chatId) || discovered.ContainsKey(chatId))
            return;

        try
        {
            var chat = await rubika.GetChatAsync(chatId, cancellationToken);
            var type = chat.ChatType ?? "";
            if (!type.Equals("Group", StringComparison.OrdinalIgnoreCase)
                && !type.Equals("Channel", StringComparison.OrdinalIgnoreCase))
                return;

            discovered[chatId] = new RubikaDiscoveredChatResponse
            {
                ChatId = chat.ChatId ?? chatId,
                Title = string.IsNullOrWhiteSpace(chat.Title) ? null : chat.Title.Trim(),
                ChatType = type,
                Username = string.IsNullOrWhiteSpace(chat.Username) ? null : chat.Username.Trim()
            };
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Rubika getChat failed for {ChatId}", chatId);
        }
    }

    async Task<List<RubikaDiscoveredChatResponse>> LoadKnownGroupChatsAsync(CancellationToken cancellationToken)
    {
        var state = await GetOrCreateStateAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(state.KnownGroupChatsJson))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<RubikaDiscoveredChatResponse>>(
                       state.KnownGroupChatsJson, KnownChatsJsonOptions)
                   ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to deserialize Rubika known group chats");
            return [];
        }
    }

    async Task SaveKnownGroupChatsAsync(
        IReadOnlyList<RubikaDiscoveredChatResponse> chats,
        CancellationToken cancellationToken)
    {
        var state = await GetOrCreateStateAsync(cancellationToken);
        var trimmed = chats
            .Where(c => !string.IsNullOrWhiteSpace(c.ChatId))
            .GroupBy(c => c.ChatId, StringComparer.Ordinal)
            .Select(g => g.First())
            .OrderByDescending(c => c.Title ?? c.ChatId)
            .Take(MaxKnownGroupChats)
            .ToList();

        state.KnownGroupChatsJson = JsonSerializer.Serialize(trimmed, KnownChatsJsonOptions);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<string?> ResolveChatIdWithSyncAsync(Person person, CancellationToken cancellationToken = default)
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

        person.RubikaChatId = null;

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
            logger.LogWarning(ex, "Rubika contact reconcile failed for person {PersonId}", person.Id);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception saveEx)
            {
                logger.LogWarning(saveEx, "Failed to save person RubikaChatId after reconcile error for person {PersonId}", person.Id);
            }
        }
    }

    async Task ApplyLocalLinkAsync(Person person, string? newPhone, CancellationToken cancellationToken)
    {
        if (newPhone is null)
            return;

        var link = await db.RubikaContactLinks.FirstOrDefaultAsync(l => l.NormalizedPhone == newPhone, cancellationToken);
        if (link is null)
            return;

        person.RubikaChatId = link.ChatId;
        link.PersonId = person.Id;
        link.LinkedAt = DateTime.UtcNow;
    }

    async Task DisassociateOldLinkAsync(int personId, string oldPhone, CancellationToken cancellationToken)
    {
        var oldLink = await db.RubikaContactLinks.FirstOrDefaultAsync(
            l => l.NormalizedPhone == oldPhone && l.PersonId == personId, cancellationToken);
        if (oldLink is not null)
            oldLink.PersonId = null;
    }

    async Task TrySendContactRequestAsync(string chatId, CancellationToken cancellationToken)
    {
        try
        {
            await rubika.SendContactRequestAsync(chatId, ContactRequestText, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send Rubika contact request to chat {ChatId}", chatId);
        }
    }

    async Task<RubikaBotState> GetOrCreateStateAsync(CancellationToken cancellationToken)
    {
        var state = await db.RubikaBotStates.OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken);
        if (state is not null)
            return state;

        state = new RubikaBotState();
        db.RubikaBotStates.Add(state);
        await db.SaveChangesAsync(cancellationToken);
        return state;
    }

    static string? ResolveChatId(RubikaUpdate update)
    {
        if (!string.IsNullOrWhiteSpace(update.ChatId))
            return update.ChatId;
        return null;
    }

    static string? ResolvePhone(RubikaIncomingMessage? message)
    {
        if (message is null)
            return null;

        if (!string.IsNullOrWhiteSpace(message.Contact?.PhoneNumber))
            return message.Contact.PhoneNumber;

        if (string.Equals(message.AuxData?.ButtonId, SharePhoneButtonId, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(message.Text)
            && PhoneNormalizeHelper.Normalize(message.Text) is not null)
            return message.Text;

        return null;
    }
}
