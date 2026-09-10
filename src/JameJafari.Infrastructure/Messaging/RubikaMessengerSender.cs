using JameJafari.Core.Enums;
using JameJafari.Core.Options;
using JameJafari.Infrastructure.Bale;
using JameJafari.Infrastructure.Rubika;
using Microsoft.Extensions.Options;

namespace JameJafari.Infrastructure.Messaging;

public class RubikaMessengerSender(RubikaBotClient rubika, IOptions<RubikaOptions> options) : IMessengerSender
{
    private readonly RubikaOptions _options = options.Value;

    public MessengerKind Kind => MessengerKind.Rubika;

    public bool IsConfigured => _options.IsConfigured;

    public Task<MessengerSendResult> SendAsync(MessengerSendRequest request, CancellationToken cancellationToken = default) =>
        request.MessageType switch
        {
            MessengerMessageType.Text => SendTextAsync(request, cancellationToken),
            MessengerMessageType.Photo => SendAttachmentsAsync(request, cancellationToken),
            MessengerMessageType.File => SendAttachmentsAsync(request, cancellationToken),
            _ => throw new InvalidOperationException("نوع پیام پشتیبانی نمی‌شود")
        };

    async Task<MessengerSendResult> SendTextAsync(MessengerSendRequest request, CancellationToken cancellationToken)
    {
        var messageId = await rubika.SendTextAsync(request.ChatId, request.Text!, cancellationToken);
        return new MessengerSendResult { MessageIds = [messageId] };
    }

    async Task<MessengerSendResult> SendAttachmentsAsync(MessengerSendRequest request, CancellationToken cancellationToken)
    {
        MessengerMediaHelper.ValidateAttachmentCount(request.AttachmentPaths.Count);
        var caption = request.Caption ?? request.Text;
        var ids = new List<string>(request.AttachmentPaths.Count);
        for (var i = 0; i < request.AttachmentPaths.Count; i++)
        {
            var path = request.AttachmentPaths[i];
            var fileCaption = i == 0 ? caption : null;
            ids.Add(await rubika.SendFileAsync(request.ChatId, path, fileCaption, cancellationToken));
        }

        return new MessengerSendResult { MessageIds = ids };
    }

    public Task EditAsync(MessengerEditRequest request, CancellationToken cancellationToken = default)
    {
        var text = request.MessageType == MessengerMessageType.Text
            ? request.Text!
            : request.Caption ?? request.Text!;
        return rubika.EditTextAsync(request.ChatId, request.MessageId, text, cancellationToken);
    }

    public Task DeleteAsync(MessengerDeleteRequest request, CancellationToken cancellationToken = default)
        => rubika.DeleteMessageAsync(request.ChatId, request.MessageId, cancellationToken);
}
