using JameJafari.Core.Options;
using JameJafari.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JameJafari.Api.Controllers;

[ApiController]
[Route("api/bale")]
public class BaleWebhookController(
    BaleContactSyncService syncService,
    IOptions<BaleOptions> options,
    ILogger<BaleWebhookController> logger) : ControllerBase
{
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] BaleUpdateModels.BaleWebhookUpdate update, CancellationToken cancellationToken)
    {
        var secret = options.Value.WebhookSecret;
        if (!string.IsNullOrWhiteSpace(secret))
        {
            var header = Request.Headers["X-Bale-Webhook-Secret"].FirstOrDefault();
            if (!string.Equals(header, secret, StringComparison.Ordinal))
                return Unauthorized();
        }

        try
        {
            await syncService.ProcessWebhookUpdateAsync(update, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Bale webhook processing failed");
        }

        return Ok();
    }
}
