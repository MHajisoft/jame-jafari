using JameJafari.Core.Options;
using JameJafari.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JameJafari.Api.Controllers;

[ApiController]
[Route("api/telegram")]
public class TelegramWebhookController(
    TelegramContactSyncService syncService,
    IOptions<TelegramOptions> options,
    ILogger<TelegramWebhookController> logger) : ControllerBase
{
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] TelegramUpdateModels.TelegramWebhookUpdate update, CancellationToken cancellationToken)
    {
        var secret = options.Value.WebhookSecret;
        if (!string.IsNullOrWhiteSpace(secret))
        {
            var header = Request.Headers["X-Telegram-Bot-Api-Secret-Token"].FirstOrDefault();
            if (!string.Equals(header, secret, StringComparison.Ordinal))
                return Unauthorized();
        }

        try
        {
            await syncService.ProcessWebhookUpdateAsync(update, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Telegram webhook processing failed");
        }

        return Ok();
    }
}
