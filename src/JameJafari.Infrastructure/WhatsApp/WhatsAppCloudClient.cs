using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using JameJafari.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JameJafari.Infrastructure.WhatsApp;

public class WhatsAppCloudClient(HttpClient http, IOptions<WhatsAppOptions> options, ILogger<WhatsAppCloudClient> logger)
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    readonly WhatsAppOptions _options = options.Value;

    public async Task<WhatsAppSendResult> SendTextAsync(string toWaId, string text, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var body = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = toWaId,
            type = "text",
            text = new { preview_url = false, body = text }
        };
        return await PostMessagesAsync(body, cancellationToken);
    }

    public async Task<WhatsAppSendResult> SendImageAsync(
        string toWaId,
        Stream image,
        string fileName,
        string mimeType,
        string? caption,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var mediaId = await UploadMediaAsync(image, fileName, mimeType, cancellationToken);
        var body = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = toWaId,
            type = "image",
            image = new { id = mediaId, caption }
        };
        return await PostMessagesAsync(body, cancellationToken);
    }

    public async Task<WhatsAppSendResult> SendDocumentAsync(
        string toWaId,
        Stream document,
        string fileName,
        string mimeType,
        string? caption,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var mediaId = await UploadMediaAsync(document, fileName, mimeType, cancellationToken);
        var body = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = toWaId,
            type = "document",
            document = new { id = mediaId, caption, filename = fileName }
        };
        return await PostMessagesAsync(body, cancellationToken);
    }

    public async Task<WhatsAppSendResult> SendVideoAsync(
        string toWaId,
        Stream video,
        string fileName,
        string mimeType,
        string? caption,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var mediaId = await UploadMediaAsync(video, fileName, mimeType, cancellationToken);
        var body = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = toWaId,
            type = "video",
            video = new { id = mediaId, caption }
        };
        return await PostMessagesAsync(body, cancellationToken);
    }

    public async Task<WhatsAppSendResult> SendAudioAsync(
        string toWaId,
        Stream audio,
        string fileName,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var mediaId = await UploadMediaAsync(audio, fileName, mimeType, cancellationToken);
        var body = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = toWaId,
            type = "audio",
            audio = new { id = mediaId }
        };
        return await PostMessagesAsync(body, cancellationToken);
    }

    public bool IsValidWebhookSignature(string? signatureHeader, ReadOnlySpan<byte> payload)
    {
        if (string.IsNullOrWhiteSpace(_options.AppSecret))
            return true;
        if (string.IsNullOrWhiteSpace(signatureHeader) || !signatureHeader.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.AppSecret));
        var hash = hmac.ComputeHash(payload.ToArray());
        var expected = "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signatureHeader.Trim()));
    }

    async Task<string> UploadMediaAsync(Stream content, string fileName, string mimeType, CancellationToken cancellationToken)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("whatsapp"), "messaging_product");
        form.Add(new StringContent(mimeType), "type");
        var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
        form.Add(streamContent, "file", fileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, MediaUrl()) { Content = form };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
        using var response = await http.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("WhatsApp media upload failed: {Status} {Body}", (int)response.StatusCode, json);
            throw new InvalidOperationException(ParseError(json) ?? "آپلود رسانه واتساپ ناموفق بود");
        }

        var parsed = JsonSerializer.Deserialize<WhatsAppMediaUploadResponse>(json, JsonOptions);
        if (string.IsNullOrWhiteSpace(parsed?.Id))
            throw new InvalidOperationException("شناسه رسانه واتساپ دریافت نشد");
        return parsed.Id;
    }

    async Task<WhatsAppSendResult> PostMessagesAsync(object body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, MessagesUrl())
        {
            Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
        using var response = await http.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("WhatsApp send failed: {Status} {Body}", (int)response.StatusCode, json);
            throw new InvalidOperationException(ParseError(json) ?? "ارسال پیام واتساپ ناموفق بود");
        }

        var parsed = JsonSerializer.Deserialize<WhatsAppMessagesResponse>(json, JsonOptions);
        var messageId = parsed?.Messages?.FirstOrDefault()?.Id;
        if (string.IsNullOrWhiteSpace(messageId))
            throw new InvalidOperationException("شناسه پیام واتساپ دریافت نشد");

        var waId = parsed?.Contacts?.FirstOrDefault()?.WaId;
        return new WhatsAppSendResult { MessageId = messageId, WaId = waId };
    }

    string MessagesUrl() =>
        $"{_options.ApiBaseUrl.TrimEnd('/')}/{_options.ApiVersion.Trim('/')}/{_options.PhoneNumberId}/messages";

    string MediaUrl() =>
        $"{_options.ApiBaseUrl.TrimEnd('/')}/{_options.ApiVersion.Trim('/')}/{_options.PhoneNumberId}/media";

    void EnsureConfigured()
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("واتساپ پیکربندی نشده است");
    }

    static string? ParseError(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("error", out var err)
                && err.TryGetProperty("message", out var msg))
                return msg.GetString();
        }
        catch
        {
            // ignore
        }

        return null;
    }
}

public sealed class WhatsAppSendResult
{
    public string MessageId { get; init; } = "";
    public string? WaId { get; init; }
}

public sealed class WhatsAppMessagesResponse
{
    public List<WhatsAppMessageId>? Messages { get; set; }
    public List<WhatsAppContactRef>? Contacts { get; set; }
}

public sealed class WhatsAppMessageId
{
    public string? Id { get; set; }
}

public sealed class WhatsAppContactRef
{
    [JsonPropertyName("wa_id")]
    public string? WaId { get; set; }

    [JsonPropertyName("input")]
    public string? Input { get; set; }
}

public sealed class WhatsAppMediaUploadResponse
{
    public string? Id { get; set; }
}

public static class WhatsAppWebhookModels
{
    public class Envelope
    {
        public string? Object { get; set; }
        public List<Entry>? Entry { get; set; }
    }

    public class Entry
    {
        public List<Change>? Changes { get; set; }
    }

    public class Change
    {
        public Value? Value { get; set; }
        public string? Field { get; set; }
    }

    public class Value
    {
        public List<InboundMessage>? Messages { get; set; }
        public List<WhatsAppContactRef>? Contacts { get; set; }
    }

    public class InboundMessage
    {
        public string? From { get; set; }
        public string? Id { get; set; }
        public string? Type { get; set; }
        public InboundText? Text { get; set; }
    }

    public class InboundText
    {
        public string? Body { get; set; }
    }
}
