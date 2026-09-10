namespace JameJafari.Core.Options;

/// <summary>Shared messenger hosting settings (public HTTPS base for webhook registration).</summary>
public class MessagingOptions
{
    public const string SectionName = "Messaging";

    /// <summary>
    /// Public HTTPS origin of this API (no trailing slash), e.g. https://app.example.com.
    /// Required to register Bale/Rubika webhooks.
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    public bool HasPublicBaseUrl =>
        !string.IsNullOrWhiteSpace(PublicBaseUrl)
        && Uri.TryCreate(PublicBaseUrl.Trim(), UriKind.Absolute, out var uri)
        && uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
}