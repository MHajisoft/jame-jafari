using JameJafari.Core.Options;
using JameJafari.Infrastructure.Rubika;
using JameJafari.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JameJafari.Api.Controllers;

[ApiController]
[Route("api/rubika")]
public class RubikaWebhookController(
    RubikaContactSyncService syncService,
    IOptions<RubikaOptions> options,
    ILogger<RubikaWebhookController> logger) : ControllerBase
{
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] RubikaWebhookPayload payload, CancellationToken cancellationToken)
    {
        var secret = options.Value.WebhookSecret;
        if (!string.IsNullOrWhiteSpace(secret))
        {
            var header = Request.Headers["X-Rubika-Webhook-Secret"].FirstOrDefault();
            if (!string.Equals(header, secret, StringComparison.Ordinal))
                return Unauthorized();
        }

        if (payload.Update is null)
            return Ok();

        try
        {
            await syncService.ProcessUpdateAsync(payload.Update, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Rubika webhook processing failed");
        }

        return Ok();
    }
}
