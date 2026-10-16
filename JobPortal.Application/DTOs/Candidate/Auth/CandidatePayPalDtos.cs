namespace JobPortal.Application.DTOs.Candidate.Auth
{
    // POST /api/candidate/auth/paypal/create-order
    //
    // The price, currency and plan are always resolved server-side from the
    // visitor's detected region. The PayPal order is bound to the credential
    // the registration will later present, so it can't be used by anyone else:
    //  - OTP registration            -> OtpToken (from verify-otp)
    //  - Google / LinkedIn sign-up   -> AccessToken (the provider access token)
    public class CreateCandidatePayPalOrderRequestDto
    {
        public string? OtpToken { get; set; }

        public string? AccessToken { get; set; }

        public string? CouponCode { get; set; }
    }

    // POST /api/candidate/auth/paypal/capture
    public class CaptureCandidatePayPalRequestDto
    {
        public string PayPalOrderId { get; set; } = string.Empty;

        public string? OtpToken { get; set; }

        public string? AccessToken { get; set; }
    }

    public class CaptureCandidatePayPalResponseDto
    {
        public bool Success { get; set; }

        // True once PayPal reports both the order and its capture COMPLETED.
        public bool Captured { get; set; }

        public string PayPalOrderId { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
    }
}