using JameJafari.Core.Entities;
using JameJafari.Core.Helpers;
using JameJafari.Core.Options;
using JameJafari.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JameJafari.Infrastructure.Services;

public class WhatsAppContactResolver(AppDbContext db, IOptions<WhatsAppOptions> options)
{
    private readonly WhatsAppOptions _options = options.Value;

    public async Task<string?> ResolveChatIdAsync(Person person, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(person.WhatsAppChatId))
            return person.WhatsAppChatId;

        var phone = PhoneNormalizeHelper.Normalize(person.Mobile);
        if (phone is null)
            return null;

        var link = await db.WhatsAppContactLinks.AsNoTracking()
            .FirstOrDefaultAsync(l => l.NormalizedPhone == phone, cancellationToken);
        if (!string.IsNullOrWhiteSpace(link?.ChatId))
            return link.ChatId;

        // Phone-first: Cloud API accepts wa_id derived from mobile without prior /start.
        return PhoneNormalizeHelper.ToWhatsAppId(phone);
    }

    public async Task PersistLinkAsync(Person person, string chatId, CancellationToken cancellationToken = default)
    {
        var phone = PhoneNormalizeHelper.Normalize(person.Mobile);
        if (phone is null || string.IsNullOrWhiteSpace(chatId))
            return;

        var tracked = await db.Persons.FirstOrDefaultAsync(p => p.Id == person.Id, cancellationToken);
        if (tracked is not null)
            tracked.WhatsAppChatId = chatId;

        var link = await db.WhatsAppContactLinks.FirstOrDefaultAsync(l => l.NormalizedPhone == phone, cancellationToken);
        if (link is null)
        {
            db.WhatsAppContactLinks.Add(new WhatsAppContactLink
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

    public async Task LinkByWaIdAsync(string? waId, CancellationToken cancellationToken = default)
    {
        var chatId = PhoneNormalizeHelper.ToWhatsAppId(waId);
        if (chatId is null)
            return;

        var phone = PhoneNormalizeHelper.Normalize(waId);
        if (phone is null)
            return;

        var person = await FindPersonByPhoneAsync(phone, cancellationToken);
        var link = await db.WhatsAppContactLinks.FirstOrDefaultAsync(l => l.NormalizedPhone == phone, cancellationToken);
        if (link is null)
        {
            db.WhatsAppContactLinks.Add(new WhatsAppContactLink
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
        {
            var tracked = await db.Persons.FirstOrDefaultAsync(p => p.Id == person.Id, cancellationToken);
            if (tracked is not null)
                tracked.WhatsAppChatId = chatId;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public string BuildMissingContactMessage(Person person)
    {
        var displayName = PersonDisplayNameHelper.FormatOrNull(
            person.FirstName, person.LastName, person.NamePrefix?.Name)
            ?? "این شخص";
        return $"{displayName} شماره موبایل معتبری برای واتساپ ندارد.";
    }

    public string? BuildBotStartUrl()
    {
        // Cloud API has no t.me-style deep link from phone number id alone.
        return null;
    }

    async Task<Person?> FindPersonByPhoneAsync(string normalizedPhone, CancellationToken cancellationToken)
    {
        var persons = await db.Persons.AsNoTracking()
            .Where(p => p.Mobile != null)
            .Select(p => new { p.Id, p.Mobile, p.WhatsAppChatId })
            .ToListAsync(cancellationToken);

        return persons
            .Where(p => PhoneNormalizeHelper.Normalize(p.Mobile) == normalizedPhone)
            .Select(p => new Person { Id = p.Id, Mobile = p.Mobile, WhatsAppChatId = p.WhatsAppChatId })
            .FirstOrDefault();
    }
}
