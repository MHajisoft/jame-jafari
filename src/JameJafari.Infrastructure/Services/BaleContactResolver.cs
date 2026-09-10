using System.Text.Json.Serialization;
using JameJafari.Core.Entities;
using JameJafari.Core.Helpers;
using JameJafari.Core.Options;
using JameJafari.Infrastructure.Data;
using JameJafari.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JameJafari.Infrastructure.Services;

public class BaleContactResolver(AppDbContext db, IOptions<BaleOptions> options)
{
    private readonly BaleOptions _options = options.Value;

    public async Task<long?> ResolveChatIdAsync(Person person, CancellationToken cancellationToken = default)
    {
        if (person.BaleChatId is not null)
            return person.BaleChatId;

        var phone = PhoneNormalizeHelper.Normalize(person.Mobile);
        if (phone is null)
            return null;

        var link = await db.BaleContactLinks.AsNoTracking()
            .FirstOrDefaultAsync(l => l.NormalizedPhone == phone, cancellationToken);

        return link?.ChatId;
    }

    public async Task PersistLinkAsync(Person person, long chatId, CancellationToken cancellationToken = default)
    {
        var phone = PhoneNormalizeHelper.Normalize(person.Mobile);
        if (phone is null)
            return;

        var tracked = await db.Persons.FirstOrDefaultAsync(p => p.Id == person.Id, cancellationToken);
        if (tracked is not null)
            tracked.BaleChatId = chatId;

        var link = await db.BaleContactLinks.FirstOrDefaultAsync(l => l.NormalizedPhone == phone, cancellationToken);
        if (link is null)
        {
            db.BaleContactLinks.Add(new BaleContactLink
            {
                NormalizedPhone = phone,
                ChatId = chatId,
                PersonId = person.Id,
                LinkedAt = DateTime.UtcNow
            });
        }
        else
        {
            link.ChatId = chatId;
            link.PersonId = person.Id;
            link.LinkedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task LinkByPhoneAsync(string? rawPhone, long chatId, int? personIdHint = null, CancellationToken cancellationToken = default)
    {
        var phone = PhoneNormalizeHelper.Normalize(rawPhone);
        if (phone is null)
            return;

        Person? person = null;
        if (personIdHint is not null)
            person = await db.Persons.FirstOrDefaultAsync(p => p.Id == personIdHint.Value, cancellationToken);
        else
            person = await FindPersonByPhoneAsync(phone, cancellationToken);

        var link = await db.BaleContactLinks.FirstOrDefaultAsync(l => l.NormalizedPhone == phone, cancellationToken);
        if (link is null)
        {
            db.BaleContactLinks.Add(new BaleContactLink
            {
                NormalizedPhone = phone,
                ChatId = chatId,
                PersonId = person?.Id,
                LinkedAt = DateTime.UtcNow
            });
        }
        else
        {
            link.ChatId = chatId;
            if (person is not null)
                link.PersonId = person.Id;
            link.LinkedAt = DateTime.UtcNow;
        }

        if (person is not null)
            person.BaleChatId = chatId;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task LinkByChatAsync(long chatId, int? personId = null, CancellationToken cancellationToken = default)
    {
        Person? person = null;
        if (personId is not null)
            person = await db.Persons.FirstOrDefaultAsync(p => p.Id == personId.Value, cancellationToken);

        var link = await db.BaleContactLinks.FirstOrDefaultAsync(l => l.ChatId == chatId, cancellationToken);
        if (link is null)
        {
            db.BaleContactLinks.Add(new BaleContactLink
            {
                ChatId = chatId,
                NormalizedPhone = $"chat:{chatId}",
                PersonId = person?.Id,
                LinkedAt = DateTime.UtcNow
            });
        }
        else if (person is not null)
        {
            link.PersonId = person.Id;
            link.LinkedAt = DateTime.UtcNow;
        }

        if (person is not null)
            person.BaleChatId = chatId;

        await db.SaveChangesAsync(cancellationToken);
    }

    public string? BuildBotStartUrl(int personId)
    {
        if (string.IsNullOrWhiteSpace(_options.BotUsername))
            return null;
        return $"https://ble.ir/{_options.BotUsername.TrimStart('@')}?start=person_{personId}";
    }

    public string BuildMissingContactMessage(Person person)
    {
        var displayName = PersonDisplayNameHelper.FormatOrNull(
            person.FirstName, person.LastName, person.NamePrefix?.Name)
            ?? "این شخص";
        return $"{displayName} دریافت پیام از سیستم را تایید نکرده است.";
    }

    async Task<Person?> FindPersonByPhoneAsync(string normalizedPhone, CancellationToken cancellationToken)
    {
        var persons = await db.Persons.AsNoTracking()
            .Where(p => p.Mobile != null)
            .Select(p => new { p.Id, p.Mobile, p.BaleChatId })
            .ToListAsync(cancellationToken);

        return persons
            .Where(p => PhoneNormalizeHelper.Normalize(p.Mobile) == normalizedPhone)
            .Select(p => new Person { Id = p.Id, Mobile = p.Mobile, BaleChatId = p.BaleChatId })
            .FirstOrDefault();
    }
}

public class BaleUpdateModels
{
    public class BaleWebhookUpdate
    {
        [JsonPropertyName("update_id")]
        public int UpdateId { get; set; }

        public BaleWebhookMessage? Message { get; set; }

        [JsonPropertyName("edited_message")]
        public BaleWebhookMessage? EditedMessage { get; set; }

        public BaleWebhookMessage? ResolveMessage() => Message ?? EditedMessage;
    }

    public class BaleWebhookMessage
    {
        [JsonPropertyName("message_id")]
        public int MessageId { get; set; }

        public BaleWebhookChat? Chat { get; set; }
        public BaleWebhookContact? Contact { get; set; }
        public string? Text { get; set; }

        [JsonPropertyName("from")]
        public BaleWebhookUser? From { get; set; }
    }

    public class BaleWebhookChat
    {
        public long Id { get; set; }
        public string? Type { get; set; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }

        public string? Username { get; set; }
    }

    public class BaleWebhookContact
    {
        [JsonPropertyName("phone_number")]
        [JsonConverter(typeof(FlexibleStringJsonConverter))]
        public string? PhoneNumber { get; set; }

        [JsonPropertyName("user_id")]
        public long? UserId { get; set; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }
    }

    public class BaleWebhookUser
    {
        public long Id { get; set; }

        [JsonPropertyName("is_bot")]
        public bool IsBot { get; set; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }

        public string? Username { get; set; }

        [JsonPropertyName("language_code")]
        public string? LanguageCode { get; set; }
    }

    public class BaleUpdatesResponse
    {
        public bool Ok { get; set; }
        public List<BaleWebhookUpdate>? Result { get; set; }
    }
}
