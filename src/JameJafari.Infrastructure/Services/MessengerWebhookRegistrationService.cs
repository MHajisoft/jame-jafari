using JameJafari.Core.DTOs;
using JameJafari.Core.Enums;
using JameJafari.Core.Options;
using JameJafari.Infrastructure.Bale;
using JameJafari.Infrastructure.Messaging;
using JameJafari.Infrastructure.Rubika;
using JameJafari.Infrastructure.Telegram;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JameJafari.Infrastructure.Services;

public class MessengerWebhookRegistrationService(
    BaleBotClient bale,
    RubikaBotClient rubika,
    TelegramBotClient telegram,
    MessengerSenderResolver senderResolver,
    IOptions<BaleOptions> baleOptions,
    IOptions<RubikaOptions> rubikaOptions,
    IOptions<TelegramOptions> telegramOptions,
    IOptions<WhatsAppOptions> whatsAppOptions,
    IOptions<MessagingOptions> messagingOptions,
    ILogger<MessengerWebhookRegistrationService> logger)
{
    public const string BaleWebhookPath = "/api/bale/webhook";
    public const string RubikaWebhookPath = "/api/rubika/webhook";
    public const string TelegramWebhookPath = "/api/telegram/webhook";
    public const string WhatsAppWebhookPath = "/api/whatsapp/webhook";

    readonly MessagingOptions _messaging = messagingOptions.Value;
    readonly BaleOptions _bale = baleOptions.Value;
    readonly RubikaOptions _rubika = rubikaOptions.Value;
    readonly TelegramOptions _telegram = telegramOptions.Value;
    readonly WhatsAppOptions _whatsApp = whatsAppOptions.Value;

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

    public string? BuildTelegramWebhookUrl()
    {
        var baseUrl = NormalizePublicBaseUrl();
        return baseUrl is null ? null : $"{baseUrl}{TelegramWebhookPath}";
    }

    public string? BuildWhatsAppWebhookUrl()
    {
        var baseUrl = NormalizePublicBaseUrl();
        return baseUrl is null ? null : $"{baseUrl}{WhatsAppWebhookPath}";
    }

    public async Task<RegisterMessengerWebhooksResponse> RegisterAsync(CancellationToken cancellationToken = default)
    {
        var baseUrl = NormalizePublicBaseUrl();
        if (baseUrl is null)
            throw new InvalidOperationException(
                "آدرس عمومی HTTPS سرور تنظیم نشده است. Messaging:PublicBaseUrl یا MESSAGING_PUBLIC_BASE_URL را تنظیم کنید.");

        var baleUrl = $"{baseUrl}{BaleWebhookPath}";
        var rubikaUrl = $"{baseUrl}{RubikaWebhookPath}";
        var telegramUrl = $"{baseUrl}{TelegramWebhookPath}";
        var whatsAppUrl = $"{baseUrl}{WhatsAppWebhookPath}";
        var baleOk = false;
        var rubikaOk = false;
        var telegramOk = false;
        var whatsAppOk = false;
        string? baleError = null;
        string? rubikaError = null;
        string? telegramError = null;
        string? whatsAppError = null;

        if (_bale.IsConfigured)
        {
            try
            {
                var health = await senderResolver.Get(MessengerKind.Bale).CheckHealthAsync(cancellationToken);
                if (!health.IsAvailable)
                {
                    baleError = health.ErrorMessage;
                }
                else
                {
                    await bale.SetWebhookAsync(baleUrl, cancellationToken);
                    baleOk = true;
                    logger.LogInformation("Bale webhook registered at {Url}", baleUrl);
                }
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
                var health = await senderResolver.Get(MessengerKind.Rubika).CheckHealthAsync(cancellationToken);
                if (!health.IsAvailable)
                {
                    rubikaError = health.ErrorMessage;
                }
                else
                {
                    await rubika.UpdateReceiveUpdateEndpointAsync(rubikaUrl, cancellationToken);
                    rubikaOk = true;
                    logger.LogInformation("Rubika ReceiveUpdate endpoint registered at {Url}", rubikaUrl);
                }
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

        if (_telegram.IsConfigured)
        {
            try
            {
                var health = await senderResolver.Get(MessengerKind.Telegram).CheckHealthAsync(cancellationToken);
                if (!health.IsAvailable)
                {
                    telegramError = health.ErrorMessage;
                }
                else
                {
                    await telegram.SetWebhookAsync(telegramUrl, cancellationToken);
                    telegramOk = true;
                    logger.LogInformation("Telegram webhook registered at {Url}", telegramUrl);
                }
            }
            catch (Exception ex)
            {
                telegramError = ex.Message;
                logger.LogWarning(ex, "Telegram webhook registration failed");
            }
        }
        else
        {
            telegramError = "توکن تلگرام تنظیم نشده است";
        }

        if (_whatsApp.IsConfigured)
        {
            // Meta Cloud API webhook URL is configured in Meta Developer Console (not via setWebhook).
            // Still probe Graph so ISP/filter issues surface like other messengers.
            try
            {
                var health = await senderResolver.Get(MessengerKind.WhatsApp).CheckHealthAsync(cancellationToken);
                if (!health.IsAvailable)
                {
                    whatsAppError = health.ErrorMessage;
                }
                else
                {
                    whatsAppOk = !string.IsNullOrWhiteSpace(_whatsApp.WebhookVerifyToken);
                    whatsAppError = whatsAppOk
                        ? null
                        : "WHATSAPP_WEBHOOK_VERIFY_TOKEN را تنظیم کنید و همین آدرس را در Meta Developer → WhatsApp → Configuration ثبت کنید.";
                    if (whatsAppOk)
                        logger.LogInformation("WhatsApp webhook URL ready (configure in Meta): {Url}", whatsAppUrl);
                }
            }
            catch (Exception ex)
            {
                whatsAppError = ex.Message;
                logger.LogWarning(ex, "WhatsApp webhook health check failed");
            }
        }
        else
        {
            whatsAppError = "توکن واتساپ تنظیم نشده است";
        }

        if (!baleOk && !rubikaOk && !telegramOk && !whatsAppOk)
            throw new InvalidOperationException(
                string.Join("؛ ", new[] { baleError, rubikaError, telegramError, whatsAppError }.Where(s => !string.IsNullOrWhiteSpace(s))));

        return new RegisterMessengerWebhooksResponse
        {
            BaleRegistered = baleOk,
            RubikaRegistered = rubikaOk,
            TelegramRegistered = telegramOk,
            WhatsAppRegistered = whatsAppOk,
            BaleError = baleOk ? null : baleError,
            RubikaError = rubikaOk ? null : rubikaError,
            TelegramError = telegramOk ? null : telegramError,
            WhatsAppError = whatsAppOk ? null : whatsAppError,
            BaleWebhookUrl = baleUrl,
            RubikaWebhookUrl = rubikaUrl,
            TelegramWebhookUrl = telegramUrl,
            WhatsAppWebhookUrl = whatsAppUrl
        };
    }
}
