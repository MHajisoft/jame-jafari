using JameJafari.Core.Enums;
using JameJafari.Core.Options;
using JameJafari.Infrastructure.WhatsApp;
using Microsoft.Extensions.Options;

namespace JameJafari.Infrastructure.Messaging;

public class WhatsAppMessengerSender(WhatsAppCloudClient bot, IOptions<WhatsAppOptions> options) : IMessengerSender
{
    private readonly WhatsAppOptions _options = options.Value;

    public MessengerKind Kind => MessengerKind.WhatsApp;

    public bool IsConfigured => _options.IsConfigured;

    public Task<MessengerSendResult> SendAsync(MessengerSendRequest request, CancellationToken cancellationToken = default) =>
        request.MessageType switch
        {
            MessengerMessageType.Text => SendTextAsync(request, cancellationToken),
            MessengerMessageType.Photo => SendAttachmentsAsync(request, imageCompose: true, cancellationToken),
            MessengerMessageType.File => SendAttachmentsAsync(request, imageCompose: false, cancellationToken),
            _ => throw new InvalidOperationException("نوع پیام پشتیبانی نمی‌شود")
        };

    public Task EditAsync(MessengerEditRequest request, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("ویرایش پیام واتساپ پشتیبانی نمی‌شود");

    public Task DeleteAsync(MessengerDeleteRequest request, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("حذف پیام از گفتگوی واتساپ پشتیبانی نمی‌شود");

    async Task<MessengerSendResult> SendTextAsync(MessengerSendRequest request, CancellationToken cancellationToken)
    {
        var result = await bot.SendTextAsync(request.ChatId, request.Text!, cancellationToken);
        return ToResult(result);
    }

    async Task<MessengerSendResult> SendAttachmentsAsync(
        MessengerSendRequest request,
        bool imageCompose,
        CancellationToken cancellationToken)
    {
        var caption = request.Caption ?? request.Text;
        var ids = new List<string>();
        string? waId = null;

        for (var i = 0; i < request.AttachmentPaths.Count; i++)
        {
            var path = request.AttachmentPaths[i];
            var partCaption = i == 0 ? caption : null;
            var result = await SendSingleMediaAsync(path, request.ChatId, partCaption, imageCompose, cancellationToken);
            ids.Add(result.MessageId);
            waId ??= result.WaId;
        }

        return new MessengerSendResult
        {
            MessageIds = ids,
            ChatId = waId ?? request.ChatId
        };
    }

    async Task<WhatsAppSendResult> SendSingleMediaAsync(
        string relativePath,
        string chatId,
        string? caption,
        bool imageCompose,
        CancellationToken cancellationToken)
    {
        var fullPath = ResolveUploadPath(relativePath);
        var fileName = Path.GetFileName(fullPath);
        var mime = GuessMime(fileName);
        await using var stream = File.OpenRead(fullPath);

        var kind = MessengerMediaHelper.Classify(relativePath);
        return kind switch
        {
            MessengerMediaKind.Video => await bot.SendVideoAsync(chatId, stream, fileName, mime, caption, cancellationToken),
            MessengerMediaKind.Audio => await bot.SendAudioAsync(chatId, stream, fileName, mime, cancellationToken),
            MessengerMediaKind.Document => await bot.SendDocumentAsync(chatId, stream, fileName, mime, caption, cancellationToken),
            _ when imageCompose => await bot.SendImageAsync(chatId, stream, fileName, mime, caption, cancellationToken),
            _ => await bot.SendDocumentAsync(chatId, stream, fileName, mime, caption, cancellationToken)
        };
    }

    static MessengerSendResult ToResult(WhatsAppSendResult result) => new()
    {
        MessageIds = [result.MessageId],
        ChatId = result.WaId
    };

    static string GuessMime(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".mp4" => "video/mp4",
            ".3gp" => "video/3gpp",
            ".mp3" => "audio/mpeg",
            ".ogg" => "audio/ogg",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }

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
