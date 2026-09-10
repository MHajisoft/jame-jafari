using JameJafari.Core.DTOs;
using JameJafari.Core.Entities;
using JameJafari.Core.Enums;
using JameJafari.Core.Helpers;
using JameJafari.Core.Options;
using JameJafari.Infrastructure.Bale;
using JameJafari.Infrastructure.Data;
using JameJafari.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JameJafari.Infrastructure.Services;

public class MessengerMessageService(
    AppDbContext db,
    BaleBotClient bale,
    BaleContactResolver contactResolver,
    BaleContactSyncService syncService,
    RubikaContactResolver rubikaContactResolver,
    RubikaContactSyncService rubikaSyncService,
    MessengerSenderResolver senderResolver,
    IncomeReceiptImageService receiptImages,
    MessengerWebhookRegistrationService webhookRegistration,
    IOptions<BaleOptions> options,
    IOptions<RubikaOptions> rubikaOptions)
{
    private static readonly TimeSpan MutateWindow = TimeSpan.FromHours(48);
    private readonly BaleOptions _options = options.Value;
    private readonly RubikaOptions _rubikaOptions = rubikaOptions.Value;

    public async Task<MessengerConfigResponse> GetConfigAsync()
    {
        var username = _options.BotUsername;
        if (string.IsNullOrWhiteSpace(username) && _options.IsConfigured)
        {
            try
            {
                var me = await bale.GetMeAsync();
                username = me.Username;
            }
            catch
            {
                // optional at config read time
            }
        }

        var messengers = senderResolver.GetAll()
            .Select(s => new MessengerInfoResponse
            {
                Kind = s.Kind,
                Label = MessengerLabel(s.Kind),
                IsConfigured = s.IsConfigured
            })
            .ToList();

        var publicBase = webhookRegistration.NormalizePublicBaseUrl();
        var canRegister = publicBase is not null && messengers.Any(m => m.IsConfigured);

        return new MessengerConfigResponse
        {
            IsConfigured = messengers.Any(m => m.IsConfigured),
            BotUsername = username,
            AvailableMessengers = messengers,
            PublicBaseUrl = publicBase,
            BaleWebhookUrl = webhookRegistration.BuildBaleWebhookUrl(),
            RubikaWebhookUrl = webhookRegistration.BuildRubikaWebhookUrl(),
            CanRegisterWebhooks = canRegister
        };
    }

    public async Task<PagedResult<MessengerMessageResponse>> GetPagedAsync(int page, int pageSize)
    {
        var filter = db.MessengerMessages.AsNoTracking().AsQueryable();
        var total = await filter.CountAsync();
        var rows = await ProjectRows(
                filter.OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id)
                    .Skip((page - 1) * pageSize).Take(pageSize))
            .ToListAsync();

        return new PagedResult<MessengerMessageResponse>(rows.Select(ToDto).ToList(), total, page, pageSize);
    }

    public async Task<MessengerMessageResponse?> GetByIdAsync(int id)
    {
        var row = await ProjectRows(db.MessengerMessages.AsNoTracking().Where(m => m.Id == id))
            .FirstOrDefaultAsync();
        return row is null ? null : ToDto(row);
    }

    public async Task<SendMessageResultResponse> SendAsync(SendMessengerMessageRequest request, int userId, IReadOnlyList<string> attachmentPaths)
    {
        if (request.TargetType == MessageComposeTarget.Channel)
        {
            ValidateSendRequest(request, messengers: null, attachmentPaths);
            return await SendToChannelsAsync(request, userId, attachmentPaths);
        }

        var messengers = NormalizeMessengers(request.Messengers);
        ValidateSendRequest(request, messengers, attachmentPaths);
        foreach (var messenger in messengers)
            EnsureMessengerConfigured(messenger);

        return request.TargetType switch
        {
            MessageComposeTarget.PersonGroup => new SendMessageResultResponse
            {
                Batch = await SendToPersonGroupAsync(request, messengers, userId, attachmentPaths)
            },
            MessageComposeTarget.Person => await SendToPersonAsync(request, messengers, userId, attachmentPaths),
            _ => throw new InvalidOperationException("نوع مقصد نامعتبر است")
        };
    }

    async Task<SendMessageResultResponse> SendToChannelsAsync(
        SendMessengerMessageRequest request,
        int userId,
        IReadOnlyList<string> attachmentPaths)
    {
        var channelIds = request.MessageChannelIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        if (channelIds.Count == 0)
            throw new InvalidOperationException("انتخاب کانال الزامی است");

        var channels = await db.MessageChannels.AsNoTracking()
            .Where(c => channelIds.Contains(c.Id) && c.IsActive)
            .ToListAsync();
        if (channels.Count != channelIds.Count)
            throw new InvalidOperationException("کانال یافت نشد یا غیرفعال است");

        var ordered = channelIds
            .Select(id => channels.First(c => c.Id == id))
            .ToList();

        foreach (var channel in ordered)
            EnsureMessengerConfigured(channel.MessengerKind);

        if (ordered.Count == 1)
        {
            var message = await DispatchChannelAsync(ordered[0].MessengerKind, ordered[0], request, userId, attachmentPaths, batchId: null);
            return new SendMessageResultResponse { Message = message };
        }

        var batchId = Guid.NewGuid();
        var sent = 0;
        var failed = 0;
        foreach (var channel in ordered)
        {
            var row = await DispatchChannelAsync(channel.MessengerKind, channel, request, userId, attachmentPaths, batchId);
            if (row.Status == MessengerMessageStatus.Sent) sent++;
            else failed++;
        }

        return new SendMessageResultResponse
        {
            Batch = new SendMessageBatchResponse
            {
                BatchId = batchId,
                Total = ordered.Count,
                Sent = sent,
                Failed = failed
            }
        };
    }

    async Task<MessengerMessageResponse> DispatchChannelAsync(
        MessengerKind messenger,
        MessageChannel channel,
        SendMessengerMessageRequest request,
        int userId,
        IReadOnlyList<string> attachmentPaths,
        Guid? batchId)
    {
        var entity = CreateMessageEntity(BuildSendPlan(request, attachmentPaths), userId);
        entity.MessengerKind = messenger;
        entity.TargetKind = MessengerMessageTargetKind.Group;
        entity.MessageChannelId = channel.Id;
        entity.ChatId = channel.ExternalChatId;
        entity.BroadcastBatchId = batchId;
        db.MessengerMessages.Add(entity);
        await db.SaveChangesAsync();
        await TryDispatchAsync(entity, userId);
        return (await GetByIdAsync(entity.Id))!;
    }

    async Task<SendMessageBatchResponse> SendToPersonGroupAsync(
        SendMessengerMessageRequest request,
        IReadOnlyList<MessengerKind> messengers,
        int userId,
        IReadOnlyList<string> attachmentPaths)
    {
        var groupIds = request.PersonGroupIds.Where(id => id > 0).Distinct().ToList();
        if (groupIds.Count == 0)
            throw new InvalidOperationException("انتخاب گروه اشخاص الزامی است");

        var groups = await db.PersonGroups.AsNoTracking()
            .Include(g => g.Members)
            .ThenInclude(m => m.Person)
            .Where(g => groupIds.Contains(g.Id) && g.IsActive)
            .ToListAsync();
        if (groups.Count != groupIds.Count)
            throw new InvalidOperationException("گروه اشخاص یافت نشد یا غیرفعال است");

        // Deduplicate persons across selected groups; keep first group id for audit.
        var recipients = new Dictionary<int, (Person Person, int GroupId)>();
        foreach (var groupId in groupIds)
        {
            var group = groups.First(g => g.Id == groupId);
            foreach (var member in group.Members)
            {
                if (recipients.ContainsKey(member.PersonId)) continue;
                recipients[member.PersonId] = (member.Person, group.Id);
            }
        }

        var batchId = Guid.NewGuid();
        var sent = 0;
        var failed = 0;
        var awaitingContact = 0;
        var skippedNoMobile = 0;
        var plan = BuildSendPlan(request, attachmentPaths);

        foreach (var (person, groupId) in recipients.Values)
        {
            if (string.IsNullOrWhiteSpace(person.Mobile))
            {
                skippedNoMobile++;
                continue;
            }

            var normalizedMobile = PhoneNormalizeHelper.Normalize(person.Mobile);
            foreach (var messenger in messengers)
            {
                var chatId = await ResolvePersonChatIdAsync(messenger, person);
                var awaiting = string.IsNullOrWhiteSpace(chatId);

                var entity = CreateMessageEntity(plan, userId);
                entity.MessengerKind = messenger;
                entity.TargetKind = MessengerMessageTargetKind.Private;
                entity.PersonGroupId = groupId;
                entity.BroadcastBatchId = batchId;
                entity.PersonId = person.Id;
                entity.TargetMobile = normalizedMobile;
                entity.ChatId = chatId ?? "";

                if (awaiting)
                {
                    entity.Status = MessengerMessageStatus.AwaitingContact;
                    entity.ErrorMessage = MissingContactMessage(messenger, person);
                    awaitingContact++;
                }

                db.MessengerMessages.Add(entity);
                await db.SaveChangesAsync();

                if (awaiting)
                    continue;

                if (await TryDispatchAsync(entity, userId)) sent++;
                else failed++;
            }
        }

        return new SendMessageBatchResponse
        {
            BatchId = batchId,
            Total = recipients.Count * messengers.Count,
            Sent = sent,
            Failed = failed,
            AwaitingContact = awaitingContact,
            SkippedNoMobile = skippedNoMobile
        };
    }

    async Task<SendMessageResultResponse> SendToPersonAsync(
        SendMessengerMessageRequest request,
        IReadOnlyList<MessengerKind> messengers,
        int userId,
        IReadOnlyList<string> attachmentPaths)
    {
        var personIds = request.PersonIds.Where(id => id > 0).Distinct().ToList();
        if (personIds.Count == 0)
            throw new InvalidOperationException("انتخاب شخص الزامی است");

        var persons = await db.Persons.AsNoTracking()
            .Include(p => p.NamePrefix)
            .Where(p => personIds.Contains(p.Id))
            .ToListAsync();
        if (persons.Count != personIds.Count)
            throw new InvalidOperationException("شخص یافت نشد");

        var ordered = personIds.Select(id => persons.First(p => p.Id == id)).ToList();
        foreach (var person in ordered)
        {
            if (string.IsNullOrWhiteSpace(person.Mobile))
                throw new InvalidOperationException($"شماره موبایل «{PersonDisplayName(person)}» ثبت نشده است");
        }

        var plan = BuildSendPlan(request, attachmentPaths);
        var total = ordered.Count * messengers.Count;

        if (total == 1)
        {
            var person = ordered[0];
            var normalizedMobile = PhoneNormalizeHelper.Normalize(person.Mobile) ?? "";
            var message = await DispatchPersonAsync(messengers[0], person, normalizedMobile, plan, userId, batchId: null);
            return new SendMessageResultResponse { Message = message };
        }

        var batchId = Guid.NewGuid();
        var sent = 0;
        var failed = 0;
        var awaitingContact = 0;
        foreach (var person in ordered)
        {
            var normalizedMobile = PhoneNormalizeHelper.Normalize(person.Mobile) ?? "";
            foreach (var messenger in messengers)
            {
                var row = await DispatchPersonAsync(messenger, person, normalizedMobile, plan, userId, batchId);
                if (row.Status == MessengerMessageStatus.AwaitingContact) awaitingContact++;
                else if (row.Status == MessengerMessageStatus.Sent) sent++;
                else failed++;
            }
        }

        return new SendMessageResultResponse
        {
            Batch = new SendMessageBatchResponse
            {
                BatchId = batchId,
                Total = total,
                Sent = sent,
                Failed = failed,
                AwaitingContact = awaitingContact
            }
        };
    }

    static string PersonDisplayName(Person person) =>
        string.Join(' ', new[] { person.FirstName, person.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

    async Task<MessengerMessageResponse> DispatchPersonAsync(
        MessengerKind messenger,
        Person person,
        string normalizedMobile,
        SendPlan plan,
        int userId,
        Guid? batchId)
    {
        var chatId = await ResolvePersonChatIdAsync(messenger, person);
        var entity = CreateMessageEntity(plan, userId);
        entity.MessengerKind = messenger;
        entity.TargetKind = MessengerMessageTargetKind.Private;
        entity.PersonId = person.Id;
        entity.TargetMobile = normalizedMobile;
        entity.ChatId = chatId ?? "";
        entity.BroadcastBatchId = batchId;

        if (string.IsNullOrWhiteSpace(chatId))
        {
            entity.Status = MessengerMessageStatus.AwaitingContact;
            entity.ErrorMessage = MissingContactMessage(messenger, person);
        }

        db.MessengerMessages.Add(entity);
        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(chatId))
            await TryDispatchAsync(entity, userId);

        return (await GetByIdAsync(entity.Id))!;
    }

    static IReadOnlyList<MessengerKind> NormalizeMessengers(IEnumerable<MessengerKind>? messengers)
    {
        var list = (messengers ?? [])
            .Distinct()
            .ToList();
        if (list.Count == 0)
            throw new InvalidOperationException("انتخاب حداقل یک پیام‌رسان الزامی است");
        return list;
    }

    static SendPlan BuildSendPlan(SendMessengerMessageRequest request, IReadOnlyList<string> attachmentPaths)
    {
        var messageType = MessengerMediaHelper.InferMessageType(attachmentPaths);
        var text = Normalize(request.Text);
        return messageType == MessengerMessageType.Text
            ? new SendPlan(messageType, text, null, [])
            : new SendPlan(messageType, null, text, attachmentPaths);
    }

    sealed record SendPlan(MessengerMessageType MessageType, string? Text, string? Caption, IReadOnlyList<string> AttachmentPaths);

    static MessengerMessage CreateMessageEntity(SendPlan plan, int userId) =>
        new()
        {
            MessageType = plan.MessageType,
            Text = plan.Text,
            Caption = plan.Caption,
            AttachmentPaths = plan.AttachmentPaths.ToList(),
            Status = MessengerMessageStatus.Pending,
            CreatedById = userId
        };

    async Task<bool> TryDispatchAsync(MessengerMessage entity, int userId)
    {
        try
        {
            var sender = senderResolver.Get(entity.MessengerKind);
            var result = await sender.SendAsync(new MessengerSendRequest
            {
                MessageType = entity.MessageType,
                ChatId = entity.ChatId,
                Text = entity.Text,
                Caption = entity.Caption,
                AttachmentPaths = entity.AttachmentPaths
            });
            entity.RemoteMessageIds = result.MessageIds.ToList();
            entity.RemoteMessageId = result.MessageId;
            if (!string.IsNullOrWhiteSpace(result.ChatId))
                entity.ChatId = result.ChatId;
            entity.Status = MessengerMessageStatus.Sent;
            entity.SentAt = DateTime.UtcNow;
            entity.ErrorMessage = null;

            if (entity.PersonId is not null && !string.IsNullOrWhiteSpace(entity.ChatId))
            {
                var person = await db.Persons.FirstOrDefaultAsync(p => p.Id == entity.PersonId.Value);
                if (person is not null)
                    await PersistPersonChatLinkAsync(entity.MessengerKind, person, entity.ChatId);
            }
        }
        catch (Exception ex)
        {
            entity.Status = MessengerMessageStatus.Failed;
            entity.ErrorMessage = ex.Message;
        }

        entity.UpdatedById = userId;
        await db.SaveChangesAsync();
        return entity.Status == MessengerMessageStatus.Sent;
    }

    public async Task<MessengerMessageResponse?> UpdateAsync(int id, UpdateMessengerMessageRequest request, int userId)
    {
        var entity = await db.MessengerMessages.FirstOrDefaultAsync(m => m.Id == id);
        if (entity is null) return null;
        if (entity.PersonGroupId is not null)
            throw new InvalidOperationException("ویرایش پیام‌های ارسال گروهی پشتیبانی نمی‌شود");

        EnsureCanMutateRemote(entity);
        var sender = senderResolver.Get(entity.MessengerKind);

        string? newText = null;
        string? newCaption = null;

        switch (entity.MessageType)
        {
            case MessengerMessageType.Text:
                newText = RequireText(request.Text, "متن پیام الزامی است");
                await sender.EditAsync(new MessengerEditRequest
                {
                    MessageType = entity.MessageType,
                    ChatId = entity.ChatId,
                    MessageId = entity.RemoteMessageId!,
                    Text = newText
                });
                entity.Text = newText;
                break;
            case MessengerMessageType.Photo:
            case MessengerMessageType.File:
                if (string.IsNullOrWhiteSpace(entity.Caption))
                    throw new InvalidOperationException("ویرایش پیام بدون متن پشتیبانی نمی‌شود");
                newCaption = RequireText(request.Caption ?? request.Text, "متن پیام الزامی است");
                await sender.EditAsync(new MessengerEditRequest
                {
                    MessageType = entity.MessageType,
                    ChatId = entity.ChatId,
                    MessageId = entity.RemoteMessageId!,
                    Caption = newCaption
                });
                entity.Caption = newCaption;
                break;
            default:
                throw new InvalidOperationException("نوع پیام پشتیبانی نمی‌شود");
        }

        entity.UpdatedById = userId;
        await db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<MessengerMessageDeleteResult?> DeleteAsync(int id, string scope, int userId)
    {
        var entity = await db.MessengerMessages.FirstOrDefaultAsync(m => m.Id == id);
        if (entity is null) return null;

        if (string.Equals(scope, "local", StringComparison.OrdinalIgnoreCase))
            return await DeleteLocalAsync(entity, userId);

        await DeleteRemoteAsync(entity, userId);
        return new MessengerMessageDeleteResult();
    }

    async Task DeleteRemoteAsync(MessengerMessage entity, int userId)
    {
        if (entity.Status == MessengerMessageStatus.Deleted && entity.DeletedFromRemote)
            return;

        EnsureCanMutateRemote(entity);
        var sender = senderResolver.Get(entity.MessengerKind);
        var messageIds = entity.RemoteMessageIds.Count > 0
            ? entity.RemoteMessageIds
            : entity.RemoteMessageId is not null ? [entity.RemoteMessageId] : [];

        foreach (var messageId in messageIds)
        {
            await sender.DeleteAsync(new MessengerDeleteRequest
            {
                ChatId = entity.ChatId,
                MessageId = messageId
            });
        }
        entity.Status = MessengerMessageStatus.Deleted;
        entity.DeletedFromRemote = true;
        entity.UpdatedById = userId;
        await db.SaveChangesAsync();
    }

    async Task<MessengerMessageDeleteResult> DeleteLocalAsync(MessengerMessage entity, int userId)
    {
        if (entity.IsDeleted)
            return new MessengerMessageDeleteResult();

        var paths = GetOwnedAttachmentPaths(entity);
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedById = userId;
        await db.SaveChangesAsync();
        return new MessengerMessageDeleteResult { AttachmentPathsToDelete = paths };
    }

    static IReadOnlyList<string> GetOwnedAttachmentPaths(MessengerMessage entity)
    {
        return entity.AttachmentPaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Where(path =>
            {
                var normalized = path.Replace('\\', '/').TrimStart('/');
                return normalized.StartsWith("bale/", StringComparison.OrdinalIgnoreCase)
                       || normalized.StartsWith("rubika/", StringComparison.OrdinalIgnoreCase);
            })
            .ToList();
    }

    public async Task<IncomeReceiptSendResult> TrySendIncomeReceiptAsync(int transactionId, int userId)
    {
        var tx = await db.IncomeTransactions
            .Include(t => t.Person).ThenInclude(p => p.NamePrefix)
            .Include(t => t.CostType)
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (tx is null)
            return new IncomeReceiptSendResult { Warning = "تراکنش یافت نشد" };

        if (string.IsNullOrWhiteSpace(tx.Person.Mobile))
            return new IncomeReceiptSendResult { Warning = "شماره موبایل شخص ثبت نشده است" };

        var targets = await ResolveAllReceiptTargetsAsync(tx.Person);
        if (targets.Count == 0)
        {
            var warning = !_options.IsConfigured && !_rubikaOptions.IsConfigured
                ? "توکن پیام‌رسان تنظیم نشده است"
                : contactResolver.BuildMissingContactMessage(tx.Person);
            return new IncomeReceiptSendResult { Warning = warning };
        }

        var normalizedMobile = PhoneNormalizeHelper.Normalize(tx.Person.Mobile);
        var sentMessages = new List<MessengerMessageResponse>();
        var failures = new List<string>();

        foreach (var target in targets)
        {
            string imagePath;
            try
            {
                imagePath = receiptImages.Create(tx, target.Messenger);
            }
            catch (Exception ex)
            {
                failures.Add($"{MessengerLabel(target.Messenger)}: {(ex is InvalidOperationException ? ex.Message : "تولید تصویر رسید ناموفق بود")}");
                continue;
            }

            var entity = new MessengerMessage
            {
                MessengerKind = target.Messenger,
                MessageType = MessengerMessageType.Photo,
                TargetKind = MessengerMessageTargetKind.Private,
                ChatId = target.ChatId,
                TargetMobile = normalizedMobile,
                AttachmentPaths = [imagePath],
                PersonId = tx.PersonId,
                IncomeTransactionId = tx.Id,
                Status = MessengerMessageStatus.Pending,
                CreatedById = userId
            };

            db.MessengerMessages.Add(entity);
            await db.SaveChangesAsync();
            var sent = await TryDispatchAsync(entity, userId);
            if (sent)
                sentMessages.Add((await GetByIdAsync(entity.Id))!);
            else
                failures.Add($"{MessengerLabel(target.Messenger)}: {entity.ErrorMessage ?? "ارسال رسید ناموفق بود"}");
        }

        if (sentMessages.Count == 0)
        {
            return new IncomeReceiptSendResult
            {
                Warning = failures.Count > 0 ? string.Join("؛ ", failures) : "ارسال رسید ناموفق بود"
            };
        }

        return new IncomeReceiptSendResult
        {
            Sent = true,
            Message = sentMessages[0],
            Messages = sentMessages,
            Warning = failures.Count > 0 ? string.Join("؛ ", failures) : null
        };
    }

    async Task<IReadOnlyList<(MessengerKind Messenger, string ChatId)>> ResolveAllReceiptTargetsAsync(Person person)
    {
        var targets = new List<(MessengerKind, string)>();

        if (_options.IsConfigured)
        {
            var baleChatId = await syncService.ResolveChatIdWithSyncAsync(person);
            if (baleChatId is not null)
                targets.Add((MessengerKind.Bale, baleChatId.Value.ToString()));
        }

        if (_rubikaOptions.IsConfigured)
        {
            var rubikaChatId = await rubikaSyncService.ResolveChatIdWithSyncAsync(person);
            if (rubikaChatId is not null)
                targets.Add((MessengerKind.Rubika, rubikaChatId));
        }

        return targets;
    }

    async Task<string?> ResolvePersonChatIdAsync(MessengerKind messenger, Person person) => messenger switch
    {
        MessengerKind.Rubika => await rubikaContactResolver.ResolveChatIdAsync(person),
        _ => (await contactResolver.ResolveChatIdAsync(person))?.ToString()
    };

    async Task PersistPersonChatLinkAsync(MessengerKind messenger, Person person, string chatId)
    {
        if (messenger == MessengerKind.Rubika)
        {
            await rubikaContactResolver.PersistLinkAsync(person, chatId);
            return;
        }

        if (MessengerChatTargetHelper.TryParseNumericChatId(chatId, out var numericChatId))
            await contactResolver.PersistLinkAsync(person, numericChatId);
    }

    string MissingContactMessage(MessengerKind messenger, Person person) => messenger switch
    {
        MessengerKind.Rubika => rubikaContactResolver.BuildMissingContactMessage(person),
        _ => contactResolver.BuildMissingContactMessage(person)
    };

    string? BotStartUrl(MessengerKind messenger, int? personId) => messenger switch
    {
        MessengerKind.Rubika => rubikaContactResolver.BuildBotStartUrl(),
        _ => personId is null ? null : contactResolver.BuildBotStartUrl(personId.Value)
    };

    void EnsureMessengerConfigured(MessengerKind messenger)
    {
        var sender = senderResolver.Get(messenger);
        if (!sender.IsConfigured)
            throw new InvalidOperationException("پیام‌رسان انتخاب‌شده پیکربندی نشده است");
    }

    static void EnsureCanMutateRemote(MessengerMessage entity)
    {
        if (entity.Status != MessengerMessageStatus.Sent
            || (string.IsNullOrWhiteSpace(entity.RemoteMessageId) && entity.RemoteMessageIds.Count == 0))
            throw new InvalidOperationException("فقط پیام‌های ارسال‌شده قابل تغییر هستند");
        if (entity.SentAt is null || DateTime.UtcNow - entity.SentAt.Value > MutateWindow)
            throw new InvalidOperationException("پیام‌های قدیمی‌تر از ۴۸ ساعت قابل تغییر نیستند");
    }

    static void ValidateSendRequest(
        SendMessengerMessageRequest request,
        IReadOnlyList<MessengerKind>? messengers,
        IReadOnlyList<string> attachmentPaths)
    {
        switch (request.TargetType)
        {
            case MessageComposeTarget.Channel:
                if (request.MessageChannelIds.Count == 0)
                    throw new InvalidOperationException("انتخاب کانال الزامی است");
                break;
            case MessageComposeTarget.PersonGroup when request.PersonGroupIds.Count == 0:
                throw new InvalidOperationException("انتخاب گروه اشخاص الزامی است");
            case MessageComposeTarget.Person when request.PersonIds.Count == 0:
                throw new InvalidOperationException("انتخاب شخص الزامی است");
        }

        if (request.TargetType is MessageComposeTarget.PersonGroup or MessageComposeTarget.Person
            && (messengers is null || messengers.Count == 0))
            throw new InvalidOperationException("انتخاب حداقل یک پیام‌رسان الزامی است");

        if (attachmentPaths.Count == 0)
            RequireText(request.Text, "متن پیام الزامی است");
        else
            MessengerMediaHelper.InferMessageType(attachmentPaths);
    }

    static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    static string RequireText(string? value, string message)
    {
        var normalized = Normalize(value);
        if (normalized is null)
            throw new InvalidOperationException(message);
        return normalized;
    }

    static string MessengerLabel(MessengerKind kind) => kind switch
    {
        MessengerKind.Bale => "بله",
        MessengerKind.Rubika => "روبیکا",
        _ => kind.ToString()
    };

    private static IQueryable<MessengerMessageRow> ProjectRows(IQueryable<MessengerMessage> query) =>
        query.Select(m => new MessengerMessageRow
        {
            Id = m.Id,
            MessengerKind = m.MessengerKind,
            MessageType = m.MessageType,
            TargetKind = m.TargetKind,
            MessageChannelId = m.MessageChannelId,
            MessageChannelName = m.MessageChannel != null ? m.MessageChannel.Name : null,
            PersonGroupId = m.PersonGroupId,
            PersonGroupName = m.PersonGroup != null ? m.PersonGroup.Name : null,
            BroadcastBatchId = m.BroadcastBatchId,
            ChatId = m.ChatId,
            TargetMobile = m.TargetMobile,
            RemoteMessageId = m.RemoteMessageId,
            Text = m.Text,
            Caption = m.Caption,
            LinkUrl = m.LinkUrl,
            LinkLabel = m.LinkLabel,
            AttachmentPaths = m.AttachmentPaths,
            Status = m.Status,
            ErrorMessage = m.ErrorMessage,
            SentAt = m.SentAt,
            IncomeTransactionId = m.IncomeTransactionId,
            PersonId = m.PersonId,
            PersonFirstName = m.Person != null ? m.Person.FirstName : null,
            PersonLastName = m.Person != null ? m.Person.LastName : null,
            PersonNamePrefixName = m.Person != null && m.Person.NamePrefix != null ? m.Person.NamePrefix.Name : null,
            PersonNickName = m.Person != null ? m.Person.NickName : null,
            PersonPicturePath = m.Person != null ? m.Person.PicturePath : null,
            PersonIsDead = m.Person != null && m.Person.IsDead,
            CreatedAt = m.CreatedAt,
            CreatedByUsername = m.CreatedBy != null ? m.CreatedBy.Username : null,
            CreatedByAvatarPath = m.CreatedBy != null ? m.CreatedBy.AvatarPath : null,
            UpdatedAt = m.UpdatedAt,
            UpdatedByUsername = m.UpdatedBy != null ? m.UpdatedBy.Username : null,
            UpdatedByAvatarPath = m.UpdatedBy != null ? m.UpdatedBy.AvatarPath : null
        });

    private MessengerMessageResponse ToDto(MessengerMessageRow row)
    {
        var canMutate = row.Status == MessengerMessageStatus.Sent
            && !string.IsNullOrWhiteSpace(row.RemoteMessageId)
            && row.SentAt is not null
            && DateTime.UtcNow - row.SentAt.Value <= MutateWindow
            && row.PersonGroupId is null
            && (row.MessageType == MessengerMessageType.Text || !string.IsNullOrWhiteSpace(row.Caption));

        return new MessengerMessageResponse
        {
            Id = row.Id,
            MessengerKind = row.MessengerKind,
            MessageType = row.MessageType,
            TargetKind = row.TargetKind,
            MessageChannelId = row.MessageChannelId,
            MessageChannelName = row.MessageChannelName,
            PersonGroupId = row.PersonGroupId,
            PersonGroupName = row.PersonGroupName,
            BroadcastBatchId = row.BroadcastBatchId,
            ChatId = row.ChatId,
            TargetMobile = row.TargetMobile,
            RemoteMessageId = row.RemoteMessageId,
            Text = row.Text,
            Caption = row.Caption,
            LinkUrl = row.LinkUrl,
            LinkLabel = row.LinkLabel,
            PhotoPath = row.AttachmentPaths.FirstOrDefault(),
            AttachmentPaths = row.AttachmentPaths,
            Status = row.Status,
            ErrorMessage = row.ErrorMessage,
            SentAt = row.SentAt,
            IncomeTransactionId = row.IncomeTransactionId,
            PersonId = row.PersonId,
            PersonName = row.PersonFirstName is null
                ? null
                : PersonDisplayNameHelper.Format(row.PersonFirstName, row.PersonLastName, row.PersonNamePrefixName),
            PersonSummary = row.PersonId is null
                ? null
                : new PersonSummaryResponse
                {
                    Id = row.PersonId.Value,
                    DisplayName = PersonDisplayNameHelper.Format(row.PersonFirstName, row.PersonLastName, row.PersonNamePrefixName),
                    NickName = row.PersonNickName,
                    PicturePath = row.PersonPicturePath,
                    IsDead = row.PersonIsDead
                },
            BotStartUrl = row.PersonId is not null ? BotStartUrl(row.MessengerKind, row.PersonId) : null,
            CanEdit = canMutate,
            CanDeleteRemote = canMutate,
            CanDeleteLocal = true,
            Audit = AuditHelper.FromProjection(
                row.CreatedAt,
                row.CreatedByUsername,
                row.CreatedByAvatarPath,
                row.UpdatedAt,
                row.UpdatedByUsername,
                row.UpdatedByAvatarPath)
        };
    }

    private sealed class MessengerMessageRow
    {
        public int Id { get; init; }
        public MessengerKind MessengerKind { get; init; }
        public MessengerMessageType MessageType { get; init; }
        public MessengerMessageTargetKind TargetKind { get; init; }
        public int? MessageChannelId { get; init; }
        public string? MessageChannelName { get; init; }
        public int? PersonGroupId { get; init; }
        public string? PersonGroupName { get; init; }
        public Guid? BroadcastBatchId { get; init; }
        public string ChatId { get; init; } = "";
        public string? TargetMobile { get; init; }
        public string? RemoteMessageId { get; init; }
        public string? Text { get; init; }
        public string? Caption { get; init; }
        public string? LinkUrl { get; init; }
        public string? LinkLabel { get; init; }
        public List<string> AttachmentPaths { get; init; } = [];
        public MessengerMessageStatus Status { get; init; }
        public string? ErrorMessage { get; init; }
        public DateTime? SentAt { get; init; }
        public int? IncomeTransactionId { get; init; }
        public int? PersonId { get; init; }
        public string? PersonFirstName { get; init; }
        public string? PersonLastName { get; init; }
        public string? PersonNamePrefixName { get; init; }
        public string? PersonNickName { get; init; }
        public string? PersonPicturePath { get; init; }
        public bool PersonIsDead { get; init; }
        public DateTime CreatedAt { get; init; }
        public string? CreatedByUsername { get; init; }
        public string? CreatedByAvatarPath { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public string? UpdatedByUsername { get; init; }
        public string? UpdatedByAvatarPath { get; init; }
    }
}
