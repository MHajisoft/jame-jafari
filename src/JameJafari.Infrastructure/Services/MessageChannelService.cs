using JameJafari.Core.DTOs;
using JameJafari.Core.Entities;
using JameJafari.Core.Enums;
using JameJafari.Core.Helpers;
using JameJafari.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JameJafari.Infrastructure.Services;

public class MessageChannelService(AppDbContext db, RubikaContactSyncService rubikaSync)
{
    public async Task<IReadOnlyList<MessageChannelResponse>> GetAllAsync(bool activeOnly = false)
    {
        var query = db.MessageChannels.AsNoTracking().AsQueryable();
        if (activeOnly) query = query.Where(c => c.IsActive);
        var rows = await ProjectRows(query.OrderBy(c => c.Name)).ToListAsync();
        return rows.Select(ToDto).ToList();
    }

    public async Task<PagedResult<MessageChannelResponse>> GetPagedAsync(bool activeOnly, int page, int pageSize)
    {
        var query = db.MessageChannels.AsNoTracking().AsQueryable();
        if (activeOnly) query = query.Where(c => c.IsActive);

        var total = await query.CountAsync();
        var rows = await ProjectRows(
                query.OrderBy(c => c.Name).Skip((page - 1) * pageSize).Take(pageSize))
            .ToListAsync();
        return new PagedResult<MessageChannelResponse>(rows.Select(ToDto).ToList(), total, page, pageSize);
    }

    public async Task<MessageChannelResponse?> GetByIdAsync(int id)
    {
        var row = await ProjectRows(db.MessageChannels.AsNoTracking().Where(c => c.Id == id))
            .FirstOrDefaultAsync();
        return row is null ? null : ToDto(row);
    }

    public async Task<MessageChannelResponse> CreateAsync(CreateMessageChannelRequest request, int userId)
    {
        var entity = new MessageChannel
        {
            Name = request.Name.Trim(),
            MessengerKind = request.MessengerKind,
            ExternalChatId = MessengerChatTargetHelper.Normalize(request.MessengerKind, request.ExternalChatId),
            IsActive = request.IsActive,
            CreatedById = userId
        };
        db.MessageChannels.Add(entity);
        await db.SaveChangesAsync();
        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task<MessageChannelResponse?> UpdateAsync(int id, UpdateMessageChannelRequest request, int userId)
    {
        var entity = await db.MessageChannels.FirstOrDefaultAsync(c => c.Id == id);
        if (entity is null) return null;

        entity.Name = request.Name.Trim();
        entity.MessengerKind = request.MessengerKind;
        entity.ExternalChatId = MessengerChatTargetHelper.Normalize(request.MessengerKind, request.ExternalChatId);
        entity.IsActive = request.IsActive;
        entity.UpdatedById = userId;
        await db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var entity = await db.MessageChannels.FirstOrDefaultAsync(c => c.Id == id);
        if (entity is null) return false;

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedById = userId;
        await db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Discovers Rubika Group/Channel chats from bot updates and creates MessageChannel rows for missing ones.
    /// Soft-deleted rows with the same chat id are restored. Existing active rows are left unchanged.
    /// </summary>
    public async Task<RubikaChannelSyncResult> SyncMissingRubikaAsync(int userId, CancellationToken cancellationToken = default)
    {
        var discovered = await rubikaSync.DiscoverGroupChatsAsync(cancellationToken);
        var existing = await db.MessageChannels
            .IgnoreQueryFilters()
            .Where(c => c.MessengerKind == MessengerKind.Rubika)
            .ToListAsync(cancellationToken);
        // Prefer a live row when both soft-deleted and active exist for the same chat id.
        var byChatId = existing
            .GroupBy(c => c.ExternalChatId, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(c => c.IsDeleted).ThenByDescending(c => c.Id).First(),
                StringComparer.Ordinal);

        var addedNames = new List<string>();
        var skipped = 0;
        var dirty = false;

        foreach (var chat in discovered)
        {
            string chatId;
            try
            {
                chatId = MessengerChatTargetHelper.Normalize(MessengerKind.Rubika, chat.ChatId);
            }
            catch (InvalidOperationException)
            {
                skipped++;
                continue;
            }

            var name = Truncate(
                !string.IsNullOrWhiteSpace(chat.Title)
                    ? chat.Title.Trim()
                    : DefaultRubikaChannelName(chat.ChatType, chatId),
                200);

            if (byChatId.TryGetValue(chatId, out var existingRow))
            {
                if (!existingRow.IsDeleted)
                {
                    skipped++;
                    continue;
                }

                existingRow.IsDeleted = false;
                existingRow.DeletedAt = null;
                existingRow.DeletedById = null;
                existingRow.IsActive = true;
                existingRow.Name = name;
                existingRow.UpdatedById = userId;
                dirty = true;
                addedNames.Add(name);
                continue;
            }

            var entity = new MessageChannel
            {
                Name = name,
                MessengerKind = MessengerKind.Rubika,
                ExternalChatId = chatId,
                IsActive = true,
                CreatedById = userId
            };
            db.MessageChannels.Add(entity);
            byChatId[chatId] = entity;
            addedNames.Add(name);
            dirty = true;
        }

        if (dirty)
            await db.SaveChangesAsync(cancellationToken);

        return new RubikaChannelSyncResult
        {
            Discovered = discovered.Count,
            Added = addedNames.Count,
            Skipped = skipped,
            AddedNames = addedNames
        };
    }

    static string DefaultRubikaChannelName(string? chatType, string chatId)
    {
        var kind = string.Equals(chatType, "Channel", StringComparison.OrdinalIgnoreCase) ? "کانال" : "گروه";
        var shortId = chatId.Length <= 12 ? chatId : chatId[..12];
        return $"{kind} روبیکا ({shortId})";
    }

    static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];

    public async Task<IReadOnlyList<MessageChannelLookupItemResponse>> GetLookupAsync(
        MessengerKind? messengerKind,
        bool activeOnly = true)
    {
        var query = db.MessageChannels.AsNoTracking().AsQueryable();
        if (activeOnly) query = query.Where(c => c.IsActive);
        if (messengerKind.HasValue) query = query.Where(c => c.MessengerKind == messengerKind.Value);

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new MessageChannelLookupItemResponse(c.Id, c.Name, c.MessengerKind))
            .ToListAsync();
    }

    private static IQueryable<MessageChannelRow> ProjectRows(IQueryable<MessageChannel> query) =>
        query.Select(c => new MessageChannelRow
        {
            Id = c.Id,
            Name = c.Name,
            MessengerKind = c.MessengerKind,
            ExternalChatId = c.ExternalChatId,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt,
            CreatedByUsername = c.CreatedBy != null ? c.CreatedBy.Username : null,
            CreatedByAvatarPath = c.CreatedBy != null ? c.CreatedBy.AvatarPath : null,
            UpdatedAt = c.UpdatedAt,
            UpdatedByUsername = c.UpdatedBy != null ? c.UpdatedBy.Username : null,
            UpdatedByAvatarPath = c.UpdatedBy != null ? c.UpdatedBy.AvatarPath : null
        });

    private static MessageChannelResponse ToDto(MessageChannelRow row) => new()
    {
        Id = row.Id,
        Name = row.Name,
        MessengerKind = row.MessengerKind,
        ExternalChatId = row.ExternalChatId,
        IsActive = row.IsActive,
        Audit = AuditHelper.FromProjection(
            row.CreatedAt,
            row.CreatedByUsername,
            row.CreatedByAvatarPath,
            row.UpdatedAt,
            row.UpdatedByUsername,
            row.UpdatedByAvatarPath)
    };

    private sealed class MessageChannelRow
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
        public MessengerKind MessengerKind { get; init; }
        public string ExternalChatId { get; init; } = "";
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public string? CreatedByUsername { get; init; }
        public string? CreatedByAvatarPath { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public string? UpdatedByUsername { get; init; }
        public string? UpdatedByAvatarPath { get; init; }
    }
}
