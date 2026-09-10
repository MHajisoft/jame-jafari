using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using JameJafari.Core.Options;
using JameJafari.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JameJafari.Infrastructure.Rubika;

public class RubikaBotClient(HttpClient http, IOptions<RubikaOptions> options, ILogger<RubikaBotClient> logger)
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private readonly RubikaOptions _options = options.Value;

    public async Task<RubikaBotInfo> GetMeAsync(CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var data = await PostJsonAsync<RubikaGetMeData>("getMe", new { }, cancellationToken);
        return data.Bot ?? throw new InvalidOperationException("اطلاعات بازوی روبیکا دریافت نشد");
    }

    public async Task<string> SendTextAsync(string chatId, string text, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var data = await PostJsonAsync<RubikaMessageData>("sendMessage", new { chat_id = chatId, text }, cancellationToken);
        return RequireMessageId(data.MessageId);
    }

    public async Task<string> SendContactRequestAsync(string chatId, string text, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var data = await PostJsonAsync<RubikaMessageData>("sendMessage", new
        {
            chat_id = chatId,
            text,
            chat_keypad_type = "New",
            chat_keypad = new
            {
                rows = new[]
                {
                    new
                    {
                        buttons = new[]
                        {
                            new
                            {
                                id = "share_phone",
                                type = "AskMyPhoneNumber",
                                button_text = "اشتراک شماره تماس"
                            }
                        }
                    }
                },
                resize_keyboard = true,
                one_time_keyboard = true
            }
        }, cancellationToken);
        return RequireMessageId(data.MessageId);
    }

    public async Task<string> SendFileAsync(
        string chatId,
        string relativePath,
        string? caption,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var fullPath = ResolveUploadPath(relativePath);
        var fileType = RubikaFileHelper.ApiType(MessengerMediaHelper.Classify(relativePath));
        var uploadUrl = await RequestSendFileAsync(fileType, cancellationToken);
        var fileId = await UploadFileAsync(uploadUrl, fullPath, cancellationToken);

        object body = string.IsNullOrWhiteSpace(caption)
            ? new { chat_id = chatId, file_id = fileId }
            : new { chat_id = chatId, file_id = fileId, text = caption };

        var data = await PostJsonAsync<RubikaMessageData>("sendFile", body, cancellationToken);
        return RequireMessageId(data.MessageId);
    }

    public Task EditTextAsync(string chatId, string messageId, string text, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        return PostOkAsync("editMessageText", new
        {
            chat_id = chatId,
            message_id = messageId,
            text
        }, cancellationToken);
    }

    public Task DeleteMessageAsync(string chatId, string messageId, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        return PostOkAsync("deleteMessage", new
        {
            chat_id = chatId,
            message_id = messageId
        }, cancellationToken);
    }

    public async Task<RubikaUpdatesResult> GetUpdatesAsync(
        string? offsetId = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        object body = string.IsNullOrWhiteSpace(offsetId)
            ? new { limit }
            : new { offset_id = offsetId, limit };

        return await PostJsonAsync<RubikaUpdatesResult>("getUpdates", body, cancellationToken)
               ?? new RubikaUpdatesResult();
    }

    public async Task<RubikaChatInfo> GetChatAsync(string chatId, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        if (string.IsNullOrWhiteSpace(chatId))
            throw new InvalidOperationException("شناسه گفتگو الزامی است");

        var data = await PostJsonAsync<RubikaGetChatData>("getChat", new { chat_id = chatId }, cancellationToken);
        return data.Chat ?? throw new InvalidOperationException("اطلاعات گفتگوی روبیکا دریافت نشد");
    }

    /// <summary>Registers ReceiveUpdate webhook endpoint with Rubika.</summary>
    public Task UpdateReceiveUpdateEndpointAsync(string url, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("آدرس وب‌هوک روبیکا الزامی است");

        return PostOkAsync("updateBotEndpoints", new { url, type = "ReceiveUpdate" }, cancellationToken);
    }

    async Task<string> RequestSendFileAsync(string type, CancellationToken cancellationToken)
    {
        var data = await PostJsonAsync<RubikaUploadUrlData>("requestSendFile", new { type }, cancellationToken);
        if (string.IsNullOrWhiteSpace(data.UploadUrl))
            throw new InvalidOperationException("آدرس آپلود فایل روبیکا دریافت نشد");
        return data.UploadUrl;
    }

    async Task<string> UploadFileAsync(string uploadUrl, string fullPath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(fullPath);
        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(streamContent, "file", Path.GetFileName(fullPath));

        using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl) { Content = content };
        using var response = await http.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        RubikaApiResponse<RubikaFileIdData>? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<RubikaApiResponse<RubikaFileIdData>>(raw, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Rubika upload returned non-JSON response");
            throw new InvalidOperationException("پاسخ نامعتبر از سرور آپلود روبیکا دریافت شد");
        }

        if (!IsOk(envelope?.Status) || string.IsNullOrWhiteSpace(envelope?.Data?.FileId))
            throw new InvalidOperationException(envelope?.Message ?? "آپلود فایل در روبیکا ناموفق بود");

        return envelope.Data.FileId;
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

    void EnsureConfigured()
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("توکن بازوی روبیکا تنظیم نشده است");
    }

    async Task<T> PostJsonAsync<T>(string method, object body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl(method));
        request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

        using var response = await http.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        RubikaApiResponse<T>? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<RubikaApiResponse<T>>(raw, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Rubika API returned non-JSON response for {Method}", method);
            throw new InvalidOperationException("پاسخ نامعتبر از سرور روبیکا دریافت شد");
        }

        if (IsOk(envelope?.Status) && envelope!.Data is not null)
            return envelope.Data;

        var description = envelope?.Message;
        if (string.IsNullOrWhiteSpace(description))
            description = "ارسال پیام در روبیکا ناموفق بود";
        throw new InvalidOperationException(description);
    }

    async Task PostOkAsync(string method, object body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl(method));
        request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

        using var response = await http.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        RubikaApiResponse<object?>? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<RubikaApiResponse<object?>>(raw, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Rubika API returned non-JSON response for {Method}", method);
            throw new InvalidOperationException("پاسخ نامعتبر از سرور روبیکا دریافت شد");
        }

        if (IsOk(envelope?.Status))
            return;

        var description = envelope?.Message;
        if (string.IsNullOrWhiteSpace(description))
            description = "عملیات در روبیکا ناموفق بود";
        throw new InvalidOperationException(description);
    }

    static bool IsOk(string? status) =>
        string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase);

    static string RequireMessageId(string? messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
            throw new InvalidOperationException("شناسه پیام روبیکا دریافت نشد");
        return messageId;
    }

    string BuildUrl(string method) =>
        $"{_options.BaseUrl.TrimEnd('/')}/{_options.BotToken}/{method}";
}

public static class RubikaFileHelper
{
    public static string ApiType(MessengerMediaKind kind) => kind switch
    {
        MessengerMediaKind.Photo => "Image",
        MessengerMediaKind.Video => "Video",
        MessengerMediaKind.Audio => "Music",
        _ => "File"
    };
}

public class RubikaApiResponse<T>
{
    public string? Status { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}

public class RubikaGetMeData
{
    public RubikaBotInfo? Bot { get; set; }
}

public class RubikaBotInfo
{
    [JsonPropertyName("bot_id")]
    public string? BotId { get; set; }

    public string? Username { get; set; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }
}

public class RubikaMessageData
{
    [JsonPropertyName("message_id")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? MessageId { get; set; }
}

public class RubikaUploadUrlData
{
    [JsonPropertyName("upload_url")]
    public string? UploadUrl { get; set; }
}

public class RubikaGetChatData
{
    public RubikaChatInfo? Chat { get; set; }
}

public class RubikaChatInfo
{
    [JsonPropertyName("chat_id")]
    public string? ChatId { get; set; }

    [JsonPropertyName("chat_type")]
    public string? ChatType { get; set; }

    public string? Title { get; set; }

    public string? Username { get; set; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }
}

public class RubikaFileIdData
{
    [JsonPropertyName("file_id")]
    public string? FileId { get; set; }
}

public class RubikaUpdatesResult
{
    public List<RubikaUpdate> Updates { get; set; } = [];

    [JsonPropertyName("next_offset_id")]
    public string? NextOffsetId { get; set; }
}

public class RubikaUpdate
{
    public string? Type { get; set; }

    [JsonPropertyName("chat_id")]
    public string? ChatId { get; set; }

    [JsonPropertyName("new_message")]
    public RubikaIncomingMessage? NewMessage { get; set; }
}

public class RubikaIncomingMessage
{
    [JsonPropertyName("message_id")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? MessageId { get; set; }

    public string? Text { get; set; }

    [JsonPropertyName("sender_id")]
    public string? SenderId { get; set; }

    public RubikaContactPayload? Contact { get; set; }

    [JsonPropertyName("aux_data")]
    public RubikaAuxData? AuxData { get; set; }
}

public class RubikaContactPayload
{
    [JsonPropertyName("phone_number")]
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }
}

public class RubikaAuxData
{
    [JsonPropertyName("start_id")]
    public string? StartId { get; set; }

    [JsonPropertyName("button_id")]
    public string? ButtonId { get; set; }
}

public class RubikaWebhookPayload
{
    public RubikaUpdate? Update { get; set; }
}
