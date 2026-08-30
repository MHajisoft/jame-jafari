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

public class BaleMessageService(
    AppDbContext db,
    BaleBotClient bale,
    BaleContactResolver contactResolver,
    BaleContactSyncService syncService,
    MessengerSenderResolver senderResolver,
    IncomeReceiptImageService receiptImages,
    IOptions<BaleOptions> options)
{
    private static readonly TimeSpan MutateWindow = TimeSpan.FromHours(48);
    private readonly BaleOptions _options = options.Value;

    public async Task<BaleConfigResponse> GetConfigAsync()
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

        return new BaleConfigResponse
        {
            IsConfigured = messengers.Any(m => m.IsConfigured),
            BotUsername = username,
            AvailableMessengers = messengers
        };
    }

    public async Task<PagedResult<BaleMessageResponse>> GetPagedAsync(int page, int pageSize)
    {
        var filter = db.BaleMessages.AsNoTracking().AsQueryable();
        var total = await filter.CountAsync();
        var rows = await ProjectRows(
                filter.OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id)
                    .Skip((page - 1) * pageSize).Take(pageSize))
            .ToListAsync();

        return new PagedResult<BaleMessageResponse>(rows.Select(ToDto).ToList(), total, page, pageSize);
    }

    public async Task<BaleMessageResponse?> GetByIdAsync(int id)
    {
        var row = await ProjectRows(db.BaleMessages.AsNoTracking().Where(m => m.Id == id))
            .FirstOrDefaultAsync();
        return row is null ? null : ToDto(row);
    }

    public async Task<SendMessageResultResponse> SendAsync(SendBaleMessageRequest request, int userId, IReadOnlyList<string> attachmentPaths)
    {
        ValidateSendRequest(request, attachmentPaths);
        EnsureMessengerConfigured(request.Messenger);

        return request.TargetType switch
        {
            MessageComposeTarget.Channel => new SendMessageResultResponse
            {
                Message = await SendToChannelAsync(request, userId, attachmentPaths)
            },
            MessageComposeTarget.PersonGroup => new SendMessageResultResponse
            {
                Batch = await SendToPersonGroupAsync(request, userId, attachmentPaths)
            },
            MessageComposeTarget.Person => new SendMessageResultResponse
            {
                Message = await SendToPersonAsync(request, userId, attachmentPaths)
            },
            _ => throw new InvalidOperationException("نوع مقصد نامعتبر است")
        };
    }

    async Task<BaleMessageResponse> SendToChannelAsync(SendBaleMessageRequest request, int userId, IReadOnlyList<string> attachmentPaths)
    {
        if (request.MessageChannelId is null)
            throw new InvalidOperationException("انتخاب کانال الزامی است");

        var channel = await db.MessageChannels.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.MessageChannelId.Value && c.IsActive);
        if (channel is null)
            throw new InvalidOperationException("کانال یافت نشد یا غیرفعال است");
        if (channel.MessengerKind != request.Messenger)
            throw new InvalidOperationException("کانال با پیام‌رسان انتخاب‌شده مطابقت ندارد");

        var entity = CreateMessageEntity(BuildSendPlan(request, attachmentPaths), userId);
        entity.MessengerKind = request.Messenger;
        entity.TargetKind = BaleMessageTargetKind.Group;
        entity.MessageChannelId = channel.Id;
        entity.ChatId = channel.ExternalChatId;
        db.BaleMessages.Add(entity);
        await db.SaveChangesAsync();
        await TryDispatchAsync(entity, userId);

        return (await GetByIdAsync(entity.Id))!;
    }

    async Task<SendMessageBatchResponse> SendToPersonGroupAsync(SendBaleMessageRequest request, int userId, IReadOnlyList<string> attachmentPaths)
    {
        if (request.PersonGroupId is null)
            throw new InvalidOperationException("انتخاب گروه اشخاص الزامی است");

        var group = await db.PersonGroups.AsNoTracking()
            .Include(g => g.Members)
            .ThenInclude(m => m.Person)
            .FirstOrDefaultAsync(g => g.Id == request.PersonGroupId.Value && g.IsActive);
        if (group is null)
            throw new InvalidOperationException("گروه اشخاص یافت نشد یا غیرفعال است");

        var batchId = Guid.NewGuid();
        var sent = 0;
        var failed = 0;
        var awaitingContact = 0;
        var skippedNoMobile = 0;

        foreach (var member in group.Members)
        {
            var person = member.Person;
            if (string.IsNullOrWhiteSpace(person.Mobile))
            {
                skippedNoMobile++;
                continue;
            }

            var normalizedMobile = PhoneNormalizeHelper.Normalize(person.Mobile);
            var chatId = await contactResolver.ResolveChatIdAsync(person);
            var awaiting = chatId is null;

            var entity = CreateMessageEntity(BuildSendPlan(request, attachmentPaths), userId);
            entity.MessengerKind = request.Messenger;
            entity.TargetKind = BaleMessageTargetKind.Private;
            entity.PersonGroupId = group.Id;
            entity.BroadcastBatchId = batchId;
            entity.PersonId = person.Id;
            entity.TargetMobile = normalizedMobile;
            entity.ChatId = chatId?.ToString() ?? "";

            if (awaiting)
            {
                entity.Status = BaleMessageStatus.AwaitingContact;
                entity.ErrorMessage = contactResolver.BuildMissingContactMessage(person);
                awaitingContact++;
            }

            db.BaleMessages.Add(entity);
            await db.SaveChangesAsync();

            if (awaiting)
                continue;

            var ok = await TryDispatchAsync(entity, userId);
            if (ok) sent++;
            else failed++;
        }

        return new SendMessageBatchResponse
        {
            BatchId = batchId,
            Total = group.Members.Count,
            Sent = sent,
            Failed = failed,
            AwaitingContact = awaitingContact,
            SkippedNoMobile = skippedNoMobile
        };
    }

    async Task<BaleMessageResponse> SendToPersonAsync(SendBaleMessageRequest request, int userId, IReadOnlyList<string> attachmentPaths)
    {
        if (request.PersonId is null)
            throw new InvalidOperationException("انتخاب شخص الزامی است");

        var person = await db.Persons.AsNoTracking()
            .Include(p => p.NamePrefix)
            .FirstOrDefaultAsync(p => p.Id == request.PersonId.Value);
        if (person is null)
            throw new InvalidOperationException("شخص یافت نشد");
        if (string.IsNullOrWhiteSpace(person.Mobile))
            throw new InvalidOperationException("شماره موبایل شخص ثبت نشده است");

        var normalizedMobile = PhoneNormalizeHelper.Normalize(person.Mobile);
        var chatId = await contactResolver.ResolveChatIdAsync(person);

        var plan = BuildSendPlan(request, attachmentPaths);
        var entity = CreateMessageEntity(plan, userId);
        entity.MessengerKind = request.Messenger;
        entity.TargetKind = BaleMessageTargetKind.Private;
        entity.PersonId = person.Id;
        entity.TargetMobile = normalizedMobile;
        entity.ChatId = chatId?.ToString() ?? "";

        if (chatId is null)
        {
            entity.Status = BaleMessageStatus.AwaitingContact;
            entity.ErrorMessage = contactResolver.BuildMissingContactMessage(person);
        }

        db.BaleMessages.Add(entity);
        await db.SaveChangesAsync();

        if (chatId is not null)
            await TryDispatchAsync(entity, userId);

        return (await GetByIdAsync(entity.Id))!;
    }

    static SendPlan BuildSendPlan(SendBaleMessageRequest request, IReadOnlyList<string> attachmentPaths)
    {
        var messageType = BaleMediaHelper.InferMessageType(attachmentPaths);
        var text = Normalize(request.Text);
        return messageType == BaleMessageType.Text
            ? new SendPlan(messageType, text, null, [])
            : new SendPlan(messageType, null, text, attachmentPaths);
    }

    sealed record SendPlan(BaleMessageType MessageType, string? Text, string? Caption, IReadOnlyList<string> AttachmentPaths);

    static BaleMessage CreateMessageEntity(SendPlan plan, int userId) =>
        new()
        {
            MessageType = plan.MessageType,
            Text = plan.Text,
            Caption = plan.Caption,
            AttachmentPaths = plan.AttachmentPaths.ToList(),
            Status = BaleMessageStatus.Pending,
            CreatedById = userId
        };

    async Task<bool> TryDispatchAsync(BaleMessage entity, int userId)
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
            entity.BaleMessageIds = result.MessageIds.ToList();
            entity.BaleMessageId = result.MessageId > 0 ? result.MessageId : null;
            entity.ChatId = result.Chat?.Id.ToString() ?? entity.ChatId;
            entity.Status = BaleMessageStatus.Sent;
            entity.SentAt = DateTime.UtcNow;
            entity.ErrorMessage = null;

            if (entity.PersonId is not null && BaleChatTargetHelper.TryParseNumericChatId(entity.ChatId, out var numericChatId))
            {
                var person = await db.Persons.FirstOrDefaultAsync(p => p.Id == entity.PersonId.Value);
                if (person is not null)
                    await contactResolver.PersistLinkAsync(person, numericChatId);
            }
        }
        catch (Exception ex)
        {
            entity.Status = BaleMessageStatus.Failed;
            entity.ErrorMessage = ex.Message;
        }

        entity.UpdatedById = userId;
        await db.SaveChangesAsync();
        return entity.Status == BaleMessageStatus.Sent;
    }

    public async Task<BaleMessageResponse?> UpdateAsync(int id, UpdateBaleMessageRequest request, int userId)
    {
        var entity = await db.BaleMessages.FirstOrDefaultAsync(m => m.Id == id);
        if (entity is null) return null;
        if (entity.PersonGroupId is not null)
            throw new InvalidOperationException("ویرایش پیام‌های ارسال گروهی پشتیبانی نمی‌شود");

        EnsureCanMutateOnBale(entity);
        var sender = senderResolver.Get(entity.MessengerKind);

        string? newText = null;
        string? newCaption = null;

        switch (entity.MessageType)
        {
            case BaleMessageType.Text:
                newText = RequireText(request.Text, "متن پیام الزامی است");
                await sender.EditAsync(new MessengerEditRequest
                {
                    MessageType = entity.MessageType,
                    ChatId = entity.ChatId,
                    MessageId = entity.BaleMessageId!.Value,
                    Text = newText
                });
                entity.Text = newText;
                break;
            case BaleMessageType.Photo:
            case BaleMessageType.File:
                if (string.IsNullOrWhiteSpace(entity.Caption))
                    throw new InvalidOperationException("ویرایش پیام بدون متن پشتیبانی نمی‌شود");
                newCaption = RequireText(request.Caption ?? request.Text, "متن پیام الزامی است");
                await sender.EditAsync(new MessengerEditRequest
                {
                    MessageType = entity.MessageType,
                    ChatId = entity.ChatId,
                    MessageId = entity.BaleMessageId!.Value,
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

    public async Task<BaleMessageDeleteResult?> DeleteAsync(int id, string scope, int userId)
    {
        var entity = await db.BaleMessages.FirstOrDefaultAsync(m => m.Id == id);
        if (entity is null) return null;

        if (string.Equals(scope, "local", StringComparison.OrdinalIgnoreCase))
            return await DeleteLocalAsync(entity, userId);

        await DeleteRemoteAsync(entity, userId);
        return new BaleMessageDeleteResult();
    }

    async Task DeleteRemoteAsync(BaleMessage entity, int userId)
    {
        if (entity.Status == BaleMessageStatus.Deleted && entity.DeletedFromBale)
            return;

        EnsureCanMutateOnBale(entity);
        var sender = senderResolver.Get(entity.MessengerKind);
        var messageIds = entity.BaleMessageIds.Count > 0
            ? entity.BaleMessageIds
            : entity.BaleMessageId is not null ? [entity.BaleMessageId.Value] : [];

        foreach (var messageId in messageIds)
        {
            await sender.DeleteAsync(new MessengerDeleteRequest
            {
                ChatId = entity.ChatId,
                MessageId = messageId
            });
        }
        entity.Status = BaleMessageStatus.Deleted;
        entity.DeletedFromBale = true;
        entity.UpdatedById = userId;
        await db.SaveChangesAsync();
    }

    async Task<BaleMessageDeleteResult> DeleteLocalAsync(BaleMessage entity, int userId)
    {
        if (entity.IsDeleted)
            return new BaleMessageDeleteResult();

        var paths = GetOwnedAttachmentPaths(entity);
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedById = userId;
        await db.SaveChangesAsync();
        return new BaleMessageDeleteResult { AttachmentPathsToDelete = paths };
    }

    static IReadOnlyList<string> GetOwnedAttachmentPaths(BaleMessage entity)
    {
        return entity.AttachmentPaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Where(path => path.Replace('\\', '/').TrimStart('/').StartsWith("bale/", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<IncomeReceiptSendResult> TrySendIncomeReceiptAsync(int transactionId, int userId)
    {
        if (!_options.IsConfigured)
            return new IncomeReceiptSendResult { Warning = "توکن بازوی بله تنظیم نشده است" };

        var tx = await db.IncomeTransactions
            .Include(t => t.Person).ThenInclude(p => p.NamePrefix)
            .Include(t => t.CostType)
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (tx is null)
            return new IncomeReceiptSendResult { Warning = "تراکنش یافت نشد" };

        if (string.IsNullOrWhiteSpace(tx.Person.Mobile))
            return new IncomeReceiptSendResult { Warning = "شماره موبایل شخص ثبت نشده است" };

        var normalizedMobile = PhoneNormalizeHelper.Normalize(tx.Person.Mobile);
        var chatId = await syncService.ResolveChatIdWithSyncAsync(tx.Person);
        if (chatId is null)
            return new IncomeReceiptSendResult { Warning = contactResolver.BuildMissingContactMessage(tx.Person) };

        string imagePath;
        try
        {
            imagePath = receiptImages.Create(tx);
        }
        catch (Exception ex)
        {
            return new IncomeReceiptSendResult
            {
                Warning = ex is InvalidOperationException ? ex.Message : "تولید تصویر رسید ناموفق بود"
            };
        }

        var entity = new BaleMessage
        {
            MessengerKind = MessengerKind.Bale,
            MessageType = BaleMessageType.Photo,
            TargetKind = BaleMessageTargetKind.Private,
            ChatId = chatId.Value.ToString(),
            TargetMobile = normalizedMobile,
            AttachmentPaths = [imagePath],
            PersonId = tx.PersonId,
            IncomeTransactionId = tx.Id,
            Status = BaleMessageStatus.Pending,
            CreatedById = userId
        };

        db.BaleMessages.Add(entity);
        await db.SaveChangesAsync();
        var sent = await TryDispatchAsync(entity, userId);
        if (!sent)
            return new IncomeReceiptSendResult { Warning = entity.ErrorMessage ?? "ارسال رسید ناموفق بود" };

        return new IncomeReceiptSendResult
        {
            Sent = true,
            Message = (await GetByIdAsync(entity.Id))!
        };
    }

    void EnsureMessengerConfigured(MessengerKind messenger)
    {
        var sender = senderResolver.Get(messenger);
        if (!sender.IsConfigured)
            throw new InvalidOperationException("پیام‌رسان انتخاب‌شده پیکربندی نشده است");
    }

    static void EnsureCanMutateOnBale(BaleMessage entity)
    {
        if (entity.Status != BaleMessageStatus.Sent
            || (entity.BaleMessageId is null && entity.BaleMessageIds.Count == 0))
            throw new InvalidOperationException("فقط پیام‌های ارسال‌شده قابل تغییر در بله هستند");
        if (entity.SentAt is null || DateTime.UtcNow - entity.SentAt.Value > MutateWindow)
            throw new InvalidOperationException("پیام‌های قدیمی‌تر از ۴۸ ساعت در بله قابل تغییر نیستند");
    }

    static void ValidateSendRequest(SendBaleMessageRequest request, IReadOnlyList<string> attachmentPaths)
    {
        switch (request.TargetType)
        {
            case MessageComposeTarget.Channel when request.MessageChannelId is null:
                throw new InvalidOperationException("انتخاب کانال الزامی است");
            case MessageComposeTarget.PersonGroup when request.PersonGroupId is null:
                throw new InvalidOperationException("انتخاب گروه اشخاص الزامی است");
            case MessageComposeTarget.Person when request.PersonId is null:
                throw new InvalidOperationException("انتخاب شخص الزامی است");
        }

        if (attachmentPaths.Count == 0)
            RequireText(request.Text, "متن پیام الزامی است");
        else
            BaleMediaHelper.InferMessageType(attachmentPaths);
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
        _ => kind.ToString()
    };

    private static IQueryable<BaleMessageRow> ProjectRows(IQueryable<BaleMessage> query) =>
        query.Select(m => new BaleMessageRow
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
            BaleMessageId = m.BaleMessageId,
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

    private BaleMessageResponse ToDto(BaleMessageRow row)
    {
        var canMutate = row.Status == BaleMessageStatus.Sent
            && row.BaleMessageId is not null
            && row.SentAt is not null
            && DateTime.UtcNow - row.SentAt.Value <= MutateWindow
            && row.PersonGroupId is null
            && (row.MessageType == BaleMessageType.Text || !string.IsNullOrWhiteSpace(row.Caption));

        return new BaleMessageResponse
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
            BaleMessageId = row.BaleMessageId,
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
            BotStartUrl = row.PersonId is not null ? contactResolver.BuildBotStartUrl(row.PersonId.Value) : null,
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

    private sealed class BaleMessageRow
    {
        public int Id { get; init; }
        public MessengerKind MessengerKind { get; init; }
        public BaleMessageType MessageType { get; init; }
        public BaleMessageTargetKind TargetKind { get; init; }
        public int? MessageChannelId { get; init; }
        public string? MessageChannelName { get; init; }
        public int? PersonGroupId { get; init; }
        public string? PersonGroupName { get; init; }
        public Guid? BroadcastBatchId { get; init; }
        public string ChatId { get; init; } = "";
        public string? TargetMobile { get; init; }
        public int? BaleMessageId { get; init; }
        public string? Text { get; init; }
        public string? Caption { get; init; }
        public string? LinkUrl { get; init; }
        public string? LinkLabel { get; init; }
        public List<string> AttachmentPaths { get; init; } = [];
        public BaleMessageStatus Status { get; init; }
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
