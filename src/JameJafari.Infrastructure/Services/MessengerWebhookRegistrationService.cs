using JameJafari.Core.DTOs;
using JameJafari.Core.Options;
using JameJafari.Infrastructure.Bale;
using JameJafari.Infrastructure.Rubika;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JameJafari.Infrastructure.Services;

public class MessengerWebhookRegistrationService(
    BaleBotClient bale,
    RubikaBotClient rubika,
    IOptions<BaleOptions> baleOptions,
    IOptions<RubikaOptions> rubikaOptions,
    IOptions<MessagingOptions> messagingOptions,
    ILogger<MessengerWebhookRegistrationService> logger)
{
    public const string BaleWebhookPath = "/api/bale/webhook";
    public const string RubikaWebhookPath = "/api/rubika/webhook";

    readonly MessagingOptions _messaging = messagingOptions.Value;
    readonly BaleOptions _bale = baleOptions.Value;
    readonly RubikaOptions _rubika = rubikaOptions.Value;

    public string? NormalizePublicBaseUrl() =>
        _messaging.HasPublicBaseUrl ? _messaging.PublicBaseUrl!.Trim().TrimEnd('/') : null;

    public string? BuildBaleWebhookUrl()
    {
        var baseUrl = NormalizePublicBaseUrl();
        return baseUrl is null ? null : $"{baseUrl}{BaleWebhookPath}";
    }

    public string? BuildRubikaWebhookUrl()
    {
        var baseUrl = NormalizePublicBaseUrl();
        return baseUrl is null ? null : $"{baseUrl}{RubikaWebhookPath}";
    }

    public async Task<RegisterMessengerWebhooksResponse> RegisterAsync(CancellationToken cancellationToken = default)
    {
        var baseUrl = NormalizePublicBaseUrl();
        if (baseUrl is null)
            throw new InvalidOperationException(
                "آدرس عمومی HTTPS سرور تنظیم نشده است. Messaging:PublicBaseUrl یا MESSAGING_PUBLIC_BASE_URL را تنظیم کنید.");

        var baleUrl = $"{baseUrl}{BaleWebhookPath}";
        var rubikaUrl = $"{baseUrl}{RubikaWebhookPath}";
        var baleOk = false;
        var rubikaOk = false;
        string? baleError = null;
        string? rubikaError = null;

        if (_bale.IsConfigured)
        {
            try
            {
                await bale.SetWebhookAsync(baleUrl, cancellationToken);
                baleOk = true;
                logger.LogInformation("Bale webhook registered at {Url}", baleUrl);
            }
            catch (Exception ex)
            {
                baleError = ex.Message;
                logger.LogWarning(ex, "Bale webhook registration failed");
            }
        }
        else
        {
            baleError = "توکن بله تنظیم نشده است";
        }

        if (_rubika.IsConfigured)
        {
            try
            {
                await rubika.UpdateReceiveUpdateEndpointAsync(rubikaUrl, cancellationToken);
                rubikaOk = true;
                logger.LogInformation("Rubika ReceiveUpdate endpoint registered at {Url}", rubikaUrl);
            }
            catch (Exception ex)
            {
                rubikaError = ex.Message;
                logger.LogWarning(ex, "Rubika webhook registration failed");
            }
        }
        else
        {
            rubikaError = "توکن روبیکا تنظیم نشده است";
        }

        if (!baleOk && !rubikaOk)
            throw new InvalidOperationException(
                string.Join("؛ ", new[] { baleError, rubikaError }.Where(s => !string.IsNullOrWhiteSpace(s))));

        return new RegisterMessengerWebhooksResponse
        {
            BaleRegistered = baleOk,
            RubikaRegistered = rubikaOk,
            BaleError = baleOk ? null : baleError,
            RubikaError = rubikaOk ? null : rubikaError,
            BaleWebhookUrl = baleUrl,
            RubikaWebhookUrl = rubikaUrl
        };
    }
}
