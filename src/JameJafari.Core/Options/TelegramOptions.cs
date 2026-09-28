namespace JameJafari.Core.Options;

public class TelegramOptions
{
    public const string SectionName = "Telegram";

    public string BaseUrl { get; set; } = "https://api.telegram.org";
    public string BotToken { get; set; } = "";
    public string? BotUsername { get; set; }
    public string? WebhookSecret { get; set; }
    public string UploadsRootPath { get; set; } = "";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(BotToken);
}
