namespace JameJafari.Core.Options;

/// <summary>Meta WhatsApp Cloud API (Graph) settings.</summary>
public class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";

    public string ApiBaseUrl { get; set; } = "https://graph.facebook.com";
    public string ApiVersion { get; set; } = "v21.0";
    public string AccessToken { get; set; } = "";
    public string PhoneNumberId { get; set; } = "";
    /// <summary>Token Meta sends on GET webhook verify (<c>hub.verify_token</c>).</summary>
    public string? WebhookVerifyToken { get; set; }
    /// <summary>Optional app secret for <c>X-Hub-Signature-256</c> on POST webhook.</summary>
    public string? AppSecret { get; set; }
    public string UploadsRootPath { get; set; } = "";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(AccessToken) && !string.IsNullOrWhiteSpace(PhoneNumberId);
}
