using System.Security.Cryptography;
using System.Text;
using JameJafari.Core.Entities;
using JameJafari.Core.Helpers;
using JameJafari.Core.Options;
using JameJafari.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JameJafari.Infrastructure.Services;

public class RubikaContactResolver(AppDbContext db, IOptions<RubikaOptions> options)
{
    private readonly RubikaOptions _options = options.Value;

    public async Task<string?> ResolveChatIdAsync(Person person, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(person.RubikaChatId))
            return person.RubikaChatId;

        var phone = PhoneNormalizeHelper.Normalize(person.Mobile);
        if (phone is null)
            return null;

        var link = await db.RubikaContactLinks.AsNoTracking()
            .FirstOrDefaultAsync(l => l.NormalizedPhone == phone, cancellationToken);

        return link?.ChatId;
    }

    public async Task PersistLinkAsync(Person person, string chatId, CancellationToken cancellationToken = default)
    {
        var phone = PhoneNormalizeHelper.Normalize(person.Mobile);
        if (phone is null)
            return;

        var tracked = await db.Persons.FirstOrDefaultAsync(p => p.Id == person.Id, cancellationToken);
        if (tracked is not null)
            tracked.RubikaChatId = chatId;

        var link = await db.RubikaContactLinks.FirstOrDefaultAsync(l => l.NormalizedPhone == phone, cancellationToken);
        if (link is null)
        {
            db.RubikaContactLinks.Add(new RubikaContactLink
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

    public async Task LinkByPhoneAsync(string? rawPhone, string chatId, int? personIdHint = null, CancellationToken cancellationToken = default)
    {
        var phone = PhoneNormalizeHelper.Normalize(rawPhone);
        if (phone is null)
            return;

        Person? person = null;
        if (personIdHint is not null)
            person = await db.Persons.FirstOrDefaultAsync(p => p.Id == personIdHint.Value, cancellationToken);
        else
            person = await FindPersonByPhoneAsync(phone, cancellationToken);

        var link = await db.RubikaContactLinks.FirstOrDefaultAsync(l => l.NormalizedPhone == phone, cancellationToken);
        if (link is null)
        {
            db.RubikaContactLinks.Add(new RubikaContactLink
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
            person.RubikaChatId = chatId;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task LinkByChatAsync(string chatId, int? personId = null, CancellationToken cancellationToken = default)
    {
        Person? person = null;
        if (personId is not null)
            person = await db.Persons.FirstOrDefaultAsync(p => p.Id == personId.Value, cancellationToken);

        var link = await db.RubikaContactLinks.FirstOrDefaultAsync(l => l.ChatId == chatId, cancellationToken);
        if (link is null)
        {
            db.RubikaContactLinks.Add(new RubikaContactLink
            {
                ChatId = chatId,
                // Rubika chat ids are long; keep placeholder within NormalizedPhone max length
                NormalizedPhone = PendingPhoneKey(chatId),
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
            person.RubikaChatId = chatId;

        await db.SaveChangesAsync(cancellationToken);
    }

    public string? BuildBotStartUrl()
    {
        if (string.IsNullOrWhiteSpace(_options.BotUsername))
            return null;
        return $"https://rubika.ir/{_options.BotUsername.TrimStart('@')}";
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
            .Select(p => new { p.Id, p.Mobile, p.RubikaChatId })
            .ToListAsync(cancellationToken);

        return persons
            .Where(p => PhoneNormalizeHelper.Normalize(p.Mobile) == normalizedPhone)
            .Select(p => new Person { Id = p.Id, Mobile = p.Mobile, RubikaChatId = p.RubikaChatId })
            .FirstOrDefault();
    }

    /// <summary>Stable unique placeholder for pre-phone chat links; fits nvarchar(32).</summary>
    static string PendingPhoneKey(string chatId)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(chatId)));
        return "c:" + hash[..28];
    }
}
