using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using JameJafari.Core.Options;
using JameJafari.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JameJafari.Infrastructure.Bale;

public class BaleBotClient(HttpClient http, IOptions<BaleOptions> options, ILogger<BaleBotClient> logger)
{
    internal static readonly JsonSerializerOptions UpdateJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private static readonly JsonSerializerOptions JsonOptions = UpdateJsonOptions;

    private readonly BaleOptions _options = options.Value;

    public async Task<BaleUserResult> GetMeAsync(CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        return await PostJsonAsync<BaleUserResult>("getMe", null, cancellationToken);
    }

    public Task<BaleMessageResult> SendTextAsync(string chatId, string text, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        return PostJsonAsync<BaleMessageResult>("sendMessage", new { chat_id = chatId, text }, cancellationToken);
    }

    public Task<BaleMessageResult> SendContactRequestAsync(long chatId, string text, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        return PostJsonAsync<BaleMessageResult>("sendMessage", new
        {
            chat_id = chatId,
            text,
            reply_markup = new
            {
                keyboard = new[] { new[] { new { text = "اشتراک شماره تماس", request_contact = true } } },
                resize_keyboard = true,
                one_time_keyboard = true
            }
        }, cancellationToken);
    }

    public async Task<BaleMessageResult> SendPhotoAsync(
        string chatId,
        Stream photo,
        string fileName,
        string? caption,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(chatId), "chat_id");
        var streamContent = new StreamContent(photo);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(streamContent, "photo", fileName);
        if (!string.IsNullOrWhiteSpace(caption))
            content.Add(new StringContent(caption), "caption");

        return await PostMultipartAsync<BaleMessageResult>("sendPhoto", content, cancellationToken);
    }

    public async Task<BaleMessageResult> SendDocumentAsync(
        string chatId,
        Stream document,
        string fileName,
        string? caption,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(chatId), "chat_id");
        var streamContent = new StreamContent(document);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(streamContent, "document", fileName);
        if (!string.IsNullOrWhiteSpace(caption))
            content.Add(new StringContent(caption), "caption");

        return await PostMultipartAsync<BaleMessageResult>("sendDocument", content, cancellationToken);
    }

    public async Task<IReadOnlyList<BaleMessageResult>> SendMediaGroupAsync(
        string chatId,
        IReadOnlyList<BaleMediaGroupItem> items,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        if (items.Count is < 2 or > BaleMediaHelper.MaxGroupSize)
            throw new InvalidOperationException($"آلبوم باید بین ۲ تا {BaleMediaHelper.MaxGroupSize} فایل باشد");

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(chatId), "chat_id");

        var media = new List<Dictionary<string, object?>>(items.Count);
        var fileParts = new List<(string AttachName, string FileName, byte[] Bytes)>(items.Count);
        for (var i = 0; i < items.Count; i++)
        {
            var attachName = $"file{i}";
            var item = items[i];
            var entry = new Dictionary<string, object?>
            {
                ["type"] = BaleMediaHelper.ApiType(item.Kind),
                ["media"] = $"attach://{attachName}"
            };
            if (i == 0 && !string.IsNullOrWhiteSpace(item.Caption))
                entry["caption"] = item.Caption;
            media.Add(entry);

            var fullPath = ResolveUploadPath(item.RelativePath);
            var bytes = await File.ReadAllBytesAsync(fullPath, cancellationToken);
            fileParts.Add((attachName, Path.GetFileName(fullPath), bytes));
        }

        foreach (var (attachName, fileName, bytes) in fileParts)
        {
            var streamContent = new ByteArrayContent(bytes);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(streamContent, attachName, fileName);
        }

        content.Add(new StringContent(JsonSerializer.Serialize(media, JsonOptions)), "media");
        return await PostMultipartAsync<IReadOnlyList<BaleMessageResult>>("sendMediaGroup", content, cancellationToken);
    }

    public async Task<BaleMessageResult> SendVideoAsync(
        string chatId,
        Stream video,
        string fileName,
        string? caption,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(chatId), "chat_id");
        var streamContent = new StreamContent(video);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(streamContent, "video", fileName);
        if (!string.IsNullOrWhiteSpace(caption))
            content.Add(new StringContent(caption), "caption");

        return await PostMultipartAsync<BaleMessageResult>("sendVideo", content, cancellationToken);
    }

    public async Task<BaleMessageResult> SendAudioAsync(
        string chatId,
        Stream audio,
        string fileName,
        string? caption,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(chatId), "chat_id");
        var streamContent = new StreamContent(audio);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(streamContent, "audio", fileName);
        if (!string.IsNullOrWhiteSpace(caption))
            content.Add(new StringContent(caption), "caption");

        return await PostMultipartAsync<BaleMessageResult>("sendAudio", content, cancellationToken);
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

    public Task<BaleMessageResult> EditTextAsync(string chatId, int messageId, string text, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        return PostJsonAsync<BaleMessageResult>("editMessageText", new { chat_id = chatId, message_id = messageId, text }, cancellationToken);
    }

    public Task<BaleMessageResult> EditCaptionAsync(string chatId, int messageId, string caption, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        return PostJsonAsync<BaleMessageResult>("editMessageCaption", new { chat_id = chatId, message_id = messageId, caption }, cancellationToken);
    }

    public Task<bool> DeleteMessageAsync(string chatId, int messageId, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        return PostJsonAsync<bool>("deleteMessage", new { chat_id = chatId, message_id = messageId }, cancellationToken);
    }

    public async Task<IReadOnlyList<BaleUpdateModels.BaleWebhookUpdate>> GetUpdatesAsync(
        int? offset = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        object body = (offset, limit) switch
        {
            (null or 0, null or 0) => new { },
            (not null, null or 0) => new { offset },
            (null or 0, not null) => new { limit },
            (not null, not null) => new { offset, limit }
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl("getUpdates"));
        request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

        using var response = await http.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        var envelope = JsonSerializer.Deserialize<BaleUpdateModels.BaleUpdatesResponse>(raw, UpdateJsonOptions);
        if (envelope?.Ok != true || envelope.Result is null)
            return [];

        return envelope.Result;
    }

    void EnsureConfigured()
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("توکن بازوی بله تنظیم نشده است");
    }

    async Task<T> PostJsonAsync<T>(string method, object? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl(method));
        if (body is not null)
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return await SendAsync<T>(request, cancellationToken);
    }

    async Task<T> PostMultipartAsync<T>(string method, HttpContent content, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl(method)) { Content = content };
        return await SendAsync<T>(request, cancellationToken);
    }

    async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await http.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        BaleApiResponse<T>? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<BaleApiResponse<T>>(raw, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Bale API returned non-JSON response for {Method}", request.RequestUri);
            throw new InvalidOperationException("پاسخ نامعتبر از سرور بله دریافت شد");
        }

        if (envelope?.Ok == true && envelope.Result is not null)
            return envelope.Result;

        var description = envelope?.Description;
        if (string.IsNullOrWhiteSpace(description))
            description = "ارسال پیام در بله ناموفق بود";
        throw new InvalidOperationException(description);
    }

    string BuildUrl(string method) =>
        $"{_options.BaseUrl.TrimEnd('/')}/bot{_options.BotToken}/{method}";
}

public class BaleApiResponse<T>
{
    public bool Ok { get; set; }
    public string? Description { get; set; }
    public T? Result { get; set; }
}

public class BaleUserResult
{
    public long Id { get; set; }
    public string? Username { get; set; }
    public string? FirstName { get; set; }
}

public class BaleMessageResult
{
    [JsonPropertyName("message_id")]
    public int MessageId { get; set; }

    public BaleChatResult? Chat { get; set; }
}

public class BaleChatResult
{
    public long Id { get; set; }
}

public sealed class BaleMediaGroupItem
{
    public string RelativePath { get; init; } = "";
    public BaleMediaKind Kind { get; init; }
    public string? Caption { get; init; }
}
