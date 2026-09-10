namespace JameJafari.Core.Options;

public class RubikaOptions
{
    public const string SectionName = "Rubika";

    public string BaseUrl { get; set; } = "https://botapi.rubika.ir/v3";
    public string BotToken { get; set; } = "";
    public string? BotUsername { get; set; }
    public string? WebhookSecret { get; set; }
    public string UploadsRootPath { get; set; } = "";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(BotToken);
}
