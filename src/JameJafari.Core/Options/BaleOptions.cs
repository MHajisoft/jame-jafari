namespace JameJafari.Core.Options;

public class BaleOptions
{
    public const string SectionName = "Bale";

    public string BaseUrl { get; set; } = "https://tapi.bale.ai";
    public string BotToken { get; set; } = "";
    public long? DefaultGroupChatId { get; set; }
    public string? BotUsername { get; set; }
    public string? WebhookSecret { get; set; }
    public string UploadsRootPath { get; set; } = "";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(BotToken);
}
