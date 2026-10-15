
public class VerifyPlanPaymentRequestDto
{
    public Guid TransactionId { get; set; }

    // Razorpay verification
    public string? RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public string? RazorpaySignature { get; set; }

    // Payment gateway selection
    public string? PaymentGateway { get; set; }

    // PayPal verification
    public string? PayPalOrderId { get; set; }
}
