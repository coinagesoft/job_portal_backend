namespace JobPortal.Services.Implement;

/// <summary>
/// Store-review / payment-gateway-review accounts that skip the phone OTP.
///
///  - 9175140525 : OTP skipped for REGISTRATION and LOGIN
///  - 9359138592 : OTP skipped for LOGIN only (registration is a normal OTP)
///
/// Both are Indian (+91) numbers. For these numbers no SMS is sent. LOGIN is
/// completed directly inside send-otp (response has OtpSkipped=true + Auth.Token),
/// so the app needs no OTP screen; verify-otp still accepts ANY code as a fallback. Every other phone number and every email OTP goes through
/// the normal flow untouched.
///
/// Kill switch: set the environment variable OTP_BYPASS_DISABLED=true and
/// restart the API to turn the bypass off without a code change
/// (do this once Play Store / Razorpay review is finished).
/// </summary>
public static class OtpBypass
{
    private const string IndiaCountryCode = "91";

    private const string RegistrationAndLoginNumber = "9175140525";
    private const string LoginOnlyNumber = "9359138592";

    private static bool Disabled =>
        string.Equals(
            Environment.GetEnvironmentVariable("OTP_BYPASS_DISABLED"),
            "true",
            StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the phone OTP step of REGISTRATION is skipped.</summary>
    public static bool IsRegistrationBypass(string? countryCode, string? mobileNumber)
    {
        if (Disabled) return false;

        var mobile = NormalizeIndianMobile(countryCode, mobileNumber);
        return mobile == RegistrationAndLoginNumber;
    }

    /// <summary>True when the phone OTP step of LOGIN is skipped.</summary>
    public static bool IsLoginBypass(string? countryCode, string? mobileNumber)
    {
        if (Disabled) return false;

        var mobile = NormalizeIndianMobile(countryCode, mobileNumber);
        return mobile == RegistrationAndLoginNumber
            || mobile == LoginOnlyNumber;
    }

    // Returns the 10-digit Indian mobile, or null if this is not a +91 number.
    private static string? NormalizeIndianMobile(
        string? countryCode,
        string? mobileNumber)
    {
        if (string.IsNullOrWhiteSpace(countryCode) ||
            string.IsNullOrWhiteSpace(mobileNumber))
            return null;

        var cc = new string(countryCode.Where(char.IsDigit).ToArray());
        if (cc != IndiaCountryCode) return null;

        var digits = new string(mobileNumber.Where(char.IsDigit).ToArray());

        // tolerate a typed trunk zero ("09175140525")
        if (digits.Length == 11 && digits.StartsWith("0"))
            digits = digits.Substring(1);

        return digits.Length == 10 ? digits : null;
    }
}