using JameJafari.Core.DTOs;
using JameJafari.Core.Enums;
using JameJafari.Core.Helpers;
using JameJafari.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JameJafari.Infrastructure.Services;

public class DeathAnniversaryNotifyService(
    AppDbContext db,
    ReportService reports,
    MessengerMessageService messages,
    DeathAnniversaryCondolenceImageService images)
{
    public async Task<DeathAnniversaryNotifyResult> NotifyAsync(
        SendDeathAnniversaryNotifyRequest request,
        int userId,
        CancellationToken cancellationToken = default)
    {
        DeathAnniversaryCondolenceCopy.SelfCheck();

        var channelIds = (request.MessageChannelIds ?? [])
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        if (channelIds.Count == 0)
            throw new InvalidOperationException("انتخاب حداقل یک کانال الزامی است");

        var report = await reports.GetDeathAnniversaryReportAsync(request.Scope, request.ReferenceDate);
        if (report.Items.Count == 0)
            throw new InvalidOperationException("در این بازه کسی برای ارسال ثبت نشده است");

        var channels = await db.MessageChannels.AsNoTracking()
            .Where(c => channelIds.Contains(c.Id) && c.IsActive)
            .ToListAsync(cancellationToken);
        if (channels.Count != channelIds.Count)
            throw new InvalidOperationException("کانال یافت نشد یا غیرفعال است");

        var orderedChannels = channelIds.Select(id => channels.First(c => c.Id == id)).ToList();
        var batchId = Guid.NewGuid();
        var sent = 0;
        var failed = 0;
        var total = report.Items.Count * orderedChannels.Count;
        var failures = new List<string>();

        foreach (var person in report.Items)
        {
            var text = DeathAnniversaryCondolenceCopy.ChannelText(
                person.DisplayName, person.YearsSinceDeath);

            foreach (var channel in orderedChannels)
            {
                try
                {
                    IReadOnlyList<string> attachments = [];
                    var body = text;
                    if (request.Kind == DeathAnniversaryNotifyKind.Photo)
                    {
                        var path = images.Create(person.DisplayName, person.YearsSinceDeath, channel.MessengerKind);
                        attachments = [path];
                        body = DeathAnniversaryCondolenceCopy.ChannelCaption(
                            person.DisplayName, person.YearsSinceDeath);
                    }

                    var result = await messages.SendAsync(
                        new SendMessengerMessageRequest
                        {
                            TargetType = MessageComposeTarget.Channel,
                            MessageChannelIds = [channel.Id],
                            Text = body
                        },
                        userId,
                        attachments);

                    var ok = result.Message is not null
                        ? result.Message.Status == MessengerMessageStatus.Sent
                        : result.Batch is { Failed: 0, Sent: > 0 };

                    if (ok) sent++;
                    else
                    {
                        failed++;
                        failures.Add($"{person.DisplayName}/{channel.Name}: {result.Message?.ErrorMessage ?? "ناموفق"}");
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    failures.Add($"{person.DisplayName}/{channel.Name}: {ex.Message}");
                }
            }
        }

        return new DeathAnniversaryNotifyResult
        {
            BatchId = batchId,
            Total = total,
            Sent = sent,
            Failed = failed,
            Warning = failures.Count == 0
                ? null
                : string.Join("؛ ", failures.Take(5)) + (failures.Count > 5 ? "…" : "")
        };
    }
}
