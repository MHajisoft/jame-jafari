using System.Text;
using System.Text.Json;
using JameJafari.Core.Options;
using JameJafari.Infrastructure.Services;
using JameJafari.Infrastructure.WhatsApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JameJafari.Api.Controllers;

[ApiController]
[Route("api/whatsapp")]
public class WhatsAppWebhookController(
    WhatsAppContactSyncService syncService,
    WhatsAppCloudClient client,
    IOptions<WhatsAppOptions> options,
    ILogger<WhatsAppWebhookController> logger) : ControllerBase
{
    /// <summary>Meta webhook verification (hub.challenge).</summary>
    [HttpGet("webhook")]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        var expected = options.Value.WebhookVerifyToken;
        if (string.Equals(mode, "subscribe", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(expected)
            && string.Equals(verifyToken, expected, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(challenge))
        {
            return Content(challenge, "text/plain", Encoding.UTF8);
        }

        return Unauthorized();
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var raw = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;

        var signature = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
        if (!client.IsValidWebhookSignature(signature, Encoding.UTF8.GetBytes(raw)))
            return Unauthorized();

        WhatsAppWebhookModels.Envelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<WhatsAppWebhookModels.Envelope>(raw, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "WhatsApp webhook JSON parse failed");
            return Ok();
        }

        if (envelope is null)
            return Ok();

        try
        {
            await syncService.ProcessWebhookAsync(envelope, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "WhatsApp webhook processing failed");
        }

        return Ok();
    }
}
