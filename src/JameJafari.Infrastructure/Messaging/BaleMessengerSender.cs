using JameJafari.Core.Enums;
using JameJafari.Core.Options;
using JameJafari.Infrastructure.Bale;
using Microsoft.Extensions.Options;

namespace JameJafari.Infrastructure.Messaging;

public class BaleMessengerSender(BaleBotClient bale, IOptions<BaleOptions> options) : IMessengerSender
{
    private readonly BaleOptions _options = options.Value;

    public MessengerKind Kind => MessengerKind.Bale;

    public bool IsConfigured => _options.IsConfigured;

    public Task<MessengerSendResult> SendAsync(MessengerSendRequest request, CancellationToken cancellationToken = default) =>
        request.MessageType switch
        {
            MessengerMessageType.Text => SendTextAsync(request, cancellationToken),
            MessengerMessageType.Photo => SendAttachmentsAsync(request, imageCompose: true, cancellationToken),
            MessengerMessageType.File => SendAttachmentsAsync(request, imageCompose: false, cancellationToken),
            _ => throw new InvalidOperationException("نوع پیام پشتیبانی نمی‌شود")
        };

    async Task<MessengerSendResult> SendTextAsync(MessengerSendRequest request, CancellationToken cancellationToken) =>
        ToResult(await bale.SendTextAsync(request.ChatId, request.Text!, cancellationToken));

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
            .Select((path, index) => new BaleMediaGroupItem
            {
                RelativePath = path,
                Kind = MessengerMediaHelper.Classify(path),
                Caption = index == 0 ? caption : null
            })
            .ToList();

        var messages = await bale.SendMediaGroupAsync(request.ChatId, items, cancellationToken);
        return new MessengerSendResult
        {
            MessageIds = messages.Select(m => m.MessageId.ToString()).ToList(),
            ChatId = messages.FirstOrDefault()?.Chat?.Id.ToString()
        };
    }

    async Task<BaleMessageResult> SendSingleMediaAsync(
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
                await bale.EditTextAsync(request.ChatId, ParseBaleMessageId(request.MessageId), request.Text!, cancellationToken);
                break;
            case MessengerMessageType.Photo:
            case MessengerMessageType.File:
                await bale.EditCaptionAsync(request.ChatId, ParseBaleMessageId(request.MessageId), request.Caption ?? request.Text!, cancellationToken);
                break;
            default:
                throw new InvalidOperationException("نوع پیام پشتیبانی نمی‌شود");
        }
    }

    public Task DeleteAsync(MessengerDeleteRequest request, CancellationToken cancellationToken = default)
        => bale.DeleteMessageAsync(request.ChatId, ParseBaleMessageId(request.MessageId), cancellationToken);

    static int ParseBaleMessageId(string messageId)
    {
        if (!int.TryParse(messageId, out var id) || id <= 0)
            throw new InvalidOperationException("شناسه پیام بله نامعتبر است");
        return id;
    }

    async Task<BaleMessageResult> SendPhotoFileAsync(string chatId, string relativePath, string? caption, CancellationToken cancellationToken)
    {
        var fullPath = ResolveUploadPath(relativePath);
        await using var stream = File.OpenRead(fullPath);
        return await bale.SendPhotoAsync(chatId, stream, Path.GetFileName(fullPath), caption, cancellationToken);
    }

    async Task<BaleMessageResult> SendVideoFileAsync(string chatId, string relativePath, string? caption, CancellationToken cancellationToken)
    {
        var fullPath = ResolveUploadPath(relativePath);
        await using var stream = File.OpenRead(fullPath);
        return await bale.SendVideoAsync(chatId, stream, Path.GetFileName(fullPath), caption, cancellationToken);
    }

    async Task<BaleMessageResult> SendAudioFileAsync(string chatId, string relativePath, string? caption, CancellationToken cancellationToken)
    {
        var fullPath = ResolveUploadPath(relativePath);
        await using var stream = File.OpenRead(fullPath);
        return await bale.SendAudioAsync(chatId, stream, Path.GetFileName(fullPath), caption, cancellationToken);
    }

    async Task<BaleMessageResult> SendDocumentFileAsync(string chatId, string relativePath, string? caption, CancellationToken cancellationToken)
    {
        var fullPath = ResolveUploadPath(relativePath);
        await using var stream = File.OpenRead(fullPath);
        return await bale.SendDocumentAsync(chatId, stream, Path.GetFileName(fullPath), caption, cancellationToken);
    }

    static MessengerSendResult ToResult(BaleMessageResult result) => new()
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
