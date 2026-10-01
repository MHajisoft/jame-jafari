using JameJafari.Core.Enums;
using JameJafari.Core.Options;
using JameJafari.Infrastructure.Telegram;
using Microsoft.Extensions.Options;

namespace JameJafari.Infrastructure.Messaging;

public class TelegramMessengerSender(TelegramBotClient bot, IOptions<TelegramOptions> options) : IMessengerSender
{
    private readonly TelegramOptions _options = options.Value;

    public MessengerKind Kind => MessengerKind.Telegram;

    public bool IsConfigured => _options.IsConfigured;

    public Task<MessengerHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return Task.FromResult(MessengerHealthResult.Unavailable("تلگرام پیکربندی نشده است"));
        return MessengerHealthResult.ProbeAsync(
            "دسترسی به تلگرام برقرار نیست (احتمال فیلتر یا قطع شبکه)",
            ct => bot.GetMeAsync(ct),
            cancellationToken);
    }

    public Task<MessengerSendResult> SendAsync(MessengerSendRequest request, CancellationToken cancellationToken = default) =>
        request.MessageType switch
        {
            MessengerMessageType.Text => SendTextAsync(request, cancellationToken),
            MessengerMessageType.Photo => SendAttachmentsAsync(request, imageCompose: true, cancellationToken),
            MessengerMessageType.File => SendAttachmentsAsync(request, imageCompose: false, cancellationToken),
            _ => throw new InvalidOperationException("نوع پیام پشتیبانی نمی‌شود")
        };

    async Task<MessengerSendResult> SendTextAsync(MessengerSendRequest request, CancellationToken cancellationToken) =>
        ToResult(await bot.SendTextAsync(request.ChatId, request.Text!, cancellationToken));

    async Task<MessengerSendResult> SendAttachmentsAsync(
        MessengerSendRequest request,
        bool imageCompose,
        CancellationToken cancellationToken)
    {
        var caption = request.Caption ?? request.Text;
        if (request.AttachmentPaths.Count >= 2)
        {
            MessengerMediaHelper.ValidateMediaGroup(request.AttachmentPaths, imageCompose);
            return await SendMediaGroupAsync(request, caption, cancellationToken);
        }

        return ToResult(await SendSingleMediaAsync(
            request.AttachmentPaths[0],
            request.ChatId,
            caption,
            cancellationToken));
    }

    async Task<MessengerSendResult> SendMediaGroupAsync(
        MessengerSendRequest request,
        string? caption,
        CancellationToken cancellationToken)
    {
        var items = request.AttachmentPaths
            .Select((path, index) => new TelegramMediaGroupItem
            {
                RelativePath = path,
                Kind = MessengerMediaHelper.Classify(path),
                Caption = index == 0 ? caption : null
            })
            .ToList();

        var messages = await bot.SendMediaGroupAsync(request.ChatId, items, cancellationToken);
        return new MessengerSendResult
        {
            MessageIds = messages.Select(m => m.MessageId.ToString()).ToList(),
            ChatId = messages.FirstOrDefault()?.Chat?.Id.ToString()
        };
    }

    async Task<TelegramMessageResult> SendSingleMediaAsync(
        string relativePath,
        string chatId,
        string? caption,
        CancellationToken cancellationToken)
    {
        return MessengerMediaHelper.Classify(relativePath) switch
        {
            MessengerMediaKind.Video => await SendVideoFileAsync(chatId, relativePath, caption, cancellationToken),
            MessengerMediaKind.Audio => await SendAudioFileAsync(chatId, relativePath, caption, cancellationToken),
            MessengerMediaKind.Document => await SendDocumentFileAsync(chatId, relativePath, caption, cancellationToken),
            _ => await SendPhotoFileAsync(chatId, relativePath, caption, cancellationToken)
        };
    }

    public async Task EditAsync(MessengerEditRequest request, CancellationToken cancellationToken = default)
    {
        switch (request.MessageType)
        {
            case MessengerMessageType.Text:
                await bot.EditTextAsync(request.ChatId, ParseTelegramMessageId(request.MessageId), request.Text!, cancellationToken);
                break;
            case MessengerMessageType.Photo:
            case MessengerMessageType.File:
                await bot.EditCaptionAsync(request.ChatId, ParseTelegramMessageId(request.MessageId), request.Caption ?? request.Text!, cancellationToken);
                break;
            default:
                throw new InvalidOperationException("نوع پیام پشتیبانی نمی‌شود");
        }
    }

    public Task DeleteAsync(MessengerDeleteRequest request, CancellationToken cancellationToken = default)
        => bot.DeleteMessageAsync(request.ChatId, ParseTelegramMessageId(request.MessageId), cancellationToken);

    static int ParseTelegramMessageId(string messageId)
    {
        if (!int.TryParse(messageId, out var id) || id <= 0)
            throw new InvalidOperationException("شناسه پیام تلگرام نامعتبر است");
        return id;
    }

    async Task<TelegramMessageResult> SendPhotoFileAsync(string chatId, string relativePath, string? caption, CancellationToken cancellationToken)
    {
        var fullPath = ResolveUploadPath(relativePath);
        await using var stream = File.OpenRead(fullPath);
        return await bot.SendPhotoAsync(chatId, stream, Path.GetFileName(fullPath), caption, cancellationToken);
    }

    async Task<TelegramMessageResult> SendVideoFileAsync(string chatId, string relativePath, string? caption, CancellationToken cancellationToken)
    {
        var fullPath = ResolveUploadPath(relativePath);
        await using var stream = File.OpenRead(fullPath);
        return await bot.SendVideoAsync(chatId, stream, Path.GetFileName(fullPath), caption, cancellationToken);
    }

    async Task<TelegramMessageResult> SendAudioFileAsync(string chatId, string relativePath, string? caption, CancellationToken cancellationToken)
    {
        var fullPath = ResolveUploadPath(relativePath);
        await using var stream = File.OpenRead(fullPath);
        return await bot.SendAudioAsync(chatId, stream, Path.GetFileName(fullPath), caption, cancellationToken);
    }

    async Task<TelegramMessageResult> SendDocumentFileAsync(string chatId, string relativePath, string? caption, CancellationToken cancellationToken)
    {
        var fullPath = ResolveUploadPath(relativePath);
        await using var stream = File.OpenRead(fullPath);
        return await bot.SendDocumentAsync(chatId, stream, Path.GetFileName(fullPath), caption, cancellationToken);
    }

    static MessengerSendResult ToResult(TelegramMessageResult result) => new()
    {
        MessageIds = [result.MessageId.ToString()],
        ChatId = result.Chat?.Id.ToString()
    };

    string ResolveUploadPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(_options.UploadsRootPath))
            throw new InvalidOperationException("مسیر آپلود تنظیم نشده است");

        var combined = Path.GetFullPath(Path.Combine(
            _options.UploadsRootPath,
            relativePath.Replace('/', Path.DirectorySeparatorChar)));

        var root = Path.GetFullPath(_options.UploadsRootPath);
        if (!combined.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(combined, root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("مسیر فایل نامعتبر است");

        if (!File.Exists(combined))
            throw new InvalidOperationException("فایل یافت نشد");

        return combined;
    }
}
