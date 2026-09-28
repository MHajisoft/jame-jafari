using JameJafari.Core.Entities;
using JameJafari.Core.Helpers;
using JameJafari.Infrastructure.Data;
using JameJafari.Infrastructure.WhatsApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JameJafari.Infrastructure.Services;

public class WhatsAppContactSyncService(
    AppDbContext db,
    WhatsAppContactResolver resolver,
    ILogger<WhatsAppContactSyncService> logger)
{

    public async Task<int> ProcessWebhookAsync(WhatsAppWebhookModels.Envelope envelope, CancellationToken cancellationToken = default)
    {
        var linked = 0;
        foreach (var entry in envelope.Entry ?? [])
        {
            foreach (var change in entry.Changes ?? [])
            {
                foreach (var message in change.Value?.Messages ?? [])
                {
                    if (string.IsNullOrWhiteSpace(message.From))
                        continue;
                    try
                    {
                        await resolver.LinkByWaIdAsync(message.From, cancellationToken);
                        linked++;
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "WhatsApp link failed for wa_id {WaId}", message.From);
                    }
                }
            }
        }

        return linked;
    }

    /// <summary>No getUpdates poller on Cloud API — phone-first resolve is enough.</summary>
    public Task<int> SyncFromUpdatesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(0);

    public Task<string?> ResolveChatIdWithSyncAsync(Person person, CancellationToken cancellationToken = default)
        => resolver.ResolveChatIdAsync(person, cancellationToken);

    public async Task ReconcilePersonMobileAsync(Person person, string? previousMobile, CancellationToken cancellationToken = default)
    {
        var newPhone = PhoneNormalizeHelper.Normalize(person.Mobile);
        var oldPhone = PhoneNormalizeHelper.Normalize(previousMobile);
        if (newPhone == oldPhone)
            return;

        person.WhatsAppChatId = null;

        if (oldPhone is not null && oldPhone != newPhone)
        {
            var oldLink = await db.WhatsAppContactLinks.FirstOrDefaultAsync(
                l => l.NormalizedPhone == oldPhone && l.PersonId == person.Id, cancellationToken);
            if (oldLink is not null)
                oldLink.PersonId = null;
        }

        if (newPhone is not null)
        {
            var waId = PhoneNormalizeHelper.ToWhatsAppId(newPhone);
            if (waId is not null)
            {
                person.WhatsAppChatId = waId;
                var link = await db.WhatsAppContactLinks.FirstOrDefaultAsync(l => l.NormalizedPhone == newPhone, cancellationToken);
                if (link is null)
                {
                    db.WhatsAppContactLinks.Add(new WhatsAppContactLink
                    {
                        NormalizedPhone = newPhone,
                        ChatId = waId,
                        PersonId = person.Id,
                        LinkedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    link.ChatId = waId;
                    link.PersonId = person.Id;
                    link.LinkedAt = DateTime.UtcNow;
                }
            }
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "WhatsApp contact reconcile failed for person {PersonId}", person.Id);
        }
    }
}
