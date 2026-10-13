using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace JobPortal.Application.DTOs.Recruiter.Auth;

public class SendOtpResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? MaskedIdentifier { get; set; }  // ad***@gmail.com or ****3210
    public string IdentifierType { get; set; } = string.Empty; // "email" or "mobile"
    public int ExpiresInSeconds { get; set; } = 600;
    public int ResendCooldownSeconds { get; set; } = 60;

    // Store-review accounts only (see OtpBypass): the login was completed
    // inside send-otp, so the app must skip the OTP screen and use Auth.Token.
    // Always false / null for every other user.
    // Omitted from the JSON unless set, so every other user's response is
    // exactly what it was before.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool OtpSkipped { get; set; }

    // Registration review account only (see OtpBypass): the phone was verified
    // inside send-otp, so this is the one-time registration token the app
    // would normally get from verify-otp. Omitted from the JSON when null.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OtpToken { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AuthResponseDto? Auth { get; set; }
}