using System.Text;
using System.Text.Json;
using JobPortal.Services.Implement.Public;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Public;

/// <summary>
/// PayPal → backend webhook. Backstop for payments whose browser callback never arrived.
/// Configure in the PayPal developer dashboard (app → Webhooks):
///   URL    : https://{api-domain}/api/webhooks/paypal
///   Events : PAYMENT.CAPTURE.COMPLETED, PAYMENT.CAPTURE.DENIED, PAYMENT.CAPTURE.REFUNDED, PAYMENT.CAPTURE.REVERSED
/// then copy the Webhook ID into PayPal:WebhookId.
/// </summary>
[ApiController]
[Route("api/webhooks/paypal")]
[AllowAnonymous]
public sealed class PayPalWebhookController : ControllerBase
{
    private readonly PayPalPaymentService _payPal;
    private readonly IRecruiterCreditPlanService _plans;
    private readonly ILogger<PayPalWebhookController> _logger;

    public PayPalWebhookController(
        PayPalPaymentService payPal,
        IRecruiterCreditPlanService plans,
        ILogger<PayPalWebhookController> logger)
    {
        _payPal = payPal;
        _plans = plans;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        // Raw body is required: PayPal signs the exact bytes it sent.
        string rawBody;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            rawBody = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(rawBody))
            return BadRequest();

        var headers = Request.Headers.ToDictionary(
            h => h.Key.ToUpperInvariant(),
            h => h.Value.ToString());

        try
        {
            if (!await _payPal.VerifyWebhookSignatureAsync(headers, rawBody, cancellationToken))
            {
                _logger.LogWarning("PayPal webhook rejected: invalid signature.");
                return Unauthorized();
            }
        }
        catch (Exception ex)
        {
            // Could not reach PayPal to verify — ask PayPal to retry later.
            _logger.LogError(ex, "PayPal webhook signature check failed to run.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        JsonDocument json;
        try
        {
            json = JsonDocument.Parse(rawBody);
        }
        catch (JsonException)
        {
            return BadRequest();
        }

        using (json)
        {
            var root = json.RootElement;
            var eventType = Str(root, "event_type");

            if (!root.TryGetProperty("resource", out var resource))
                return Ok();

            // We set custom_id = our transaction id (N-format) when creating the order.
            var customId = Str(resource, "custom_id");
            var orderId = resource.TryGetProperty("supplementary_data", out var sd) &&
                          sd.TryGetProperty("related_ids", out var ids)
                ? Str(ids, "order_id")
                : null;

            if (!Guid.TryParseExact(customId, "N", out var transactionId))
            {
                // Not a payment created by this app — acknowledge so PayPal stops retrying.
                return Ok();
            }

            switch (eventType)
            {
                case "PAYMENT.CAPTURE.COMPLETED":
                    {
                        if (string.IsNullOrWhiteSpace(orderId))
                            return Ok();

                        var result = await _plans.ReconcilePayPalPaymentAsync(transactionId, orderId);

                        _logger.LogInformation(
                            "PayPal webhook {Event} txn={TxnId} → {Result}",
                            eventType, transactionId, result);

                        return result == JobPortal.Services.IImplement.IRecruiter.PayPalWebhookResult.Retry
                            ? StatusCode(StatusCodes.Status503ServiceUnavailable)
                            : Ok();
                    }

                case "PAYMENT.CAPTURE.DENIED":
                    await _plans.MarkPayPalTransactionFailedAsync(transactionId);
                    return Ok();

                case "PAYMENT.CAPTURE.REFUNDED":
                case "PAYMENT.CAPTURE.REVERSED":
                    // Whether to claw back credits is a business decision — surface it, don't guess.
                    _logger.LogCritical(
                        "PayPal {Event} for txn={TxnId}. Review and adjust the employer's credits manually if needed.",
                        eventType, transactionId);
                    return Ok();

                default:
                    return Ok();
            }
        }
    }

    private static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}