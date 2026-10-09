// ============================================================
//  JobPortal.Services/Implement/Candidate/CandidateAccountService.cs
//
//  Candidate self-service ACCOUNT DELETE.
//
//  Flow (mobile -> OTP -> delete):
//    1. POST /api/candidate/account/delete/send-otp
//         -> an OTP is sent by SMS to the mobile number registered on the
//            logged-in candidate's account.
//    2. POST /api/candidate/account/delete/confirm  { "otpCode": "123456" }
//         -> the OTP is verified; ONLY if it is valid is the account deleted.
//            There is no way to reach the delete step without a valid OTP
//            because verification and deletion happen in the same call.
//
//  This file also holds the service interface and the request/response
//  DTOs so the feature ships as two files (this + the controller).
// ============================================================

using JobPortal.Application.DTOs.Candidate.Account;
using JobPortal.Application.DTOs.Recruiter.Auth;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums;
using JobPortal.Domain.Enums.common;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

// ────────────────────────────────────────────────────────────
//  DTOs
// ────────────────────────────────────────────────────────────
namespace JobPortal.Application.DTOs.Candidate.Account
{
    public class CandidateDeleteAccountRequestDto
    {
        /// <summary>The OTP received by SMS on the registered mobile number.</summary>
        [Required(ErrorMessage = "OTP is required.")]
        [StringLength(10, MinimumLength = 4, ErrorMessage = "Invalid OTP.")]
        public string OtpCode { get; set; } = string.Empty;
    }

    /// <summary>Public (no login) delete — step 1: who to send the OTP to.</summary>
    public class CandidatePublicDeleteSendOtpRequestDto
    {
        /// <summary>Registered mobile number (digits only) or email address.</summary>
        [Required(ErrorMessage = "Mobile number or email is required.")]
        [StringLength(150)]
        public string Identifier { get; set; } = string.Empty;

        /// <summary>Required when Identifier is a mobile number, e.g. "+91".</summary>
        [StringLength(6)]
        public string? CountryCode { get; set; }
    }

    /// <summary>Public (no login) delete — step 2: OTP + same identifier.</summary>
    public class CandidatePublicDeleteConfirmRequestDto
    {
        [Required(ErrorMessage = "Mobile number or email is required.")]
        [StringLength(150)]
        public string Identifier { get; set; } = string.Empty;

        [StringLength(6)]
        public string? CountryCode { get; set; }

        [Required(ErrorMessage = "OTP is required.")]
        [StringLength(10, MinimumLength = 4, ErrorMessage = "Invalid OTP.")]
        public string OtpCode { get; set; } = string.Empty;
    }

    public class CandidateDeleteAccountResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

// ────────────────────────────────────────────────────────────
//  Interface
// ────────────────────────────────────────────────────────────
namespace JobPortal.Services.IImplement.ICandidate
{
    public interface ICandidateAccountService
    {
        /// <summary>Step 1 — sends a delete-confirmation OTP to the account's registered mobile.</summary>
        Task<SendOtpResponseDto> SendDeleteAccountOtpAsync(
            Guid candidateId,
            string ipAddress);

        /// <summary>Step 2 — verifies the OTP and, only if valid, deletes the account.</summary>
        Task<CandidateDeleteAccountResponseDto> ConfirmDeleteAccountAsync(
            Guid candidateId,
            CandidateDeleteAccountRequestDto request,
            string? jwtJti,
            DateTime? jwtExpiresAt,
            string ipAddress);

        // ── Public (NO login / token) flow, e.g. for the Play Store
        //    "delete your account" web link ──────────────────────────

        /// <summary>Sends a delete OTP to the given mobile number or email.</summary>
        Task<SendOtpResponseDto> SendPublicDeleteOtpAsync(
            CandidatePublicDeleteSendOtpRequestDto request,
            string ipAddress);

        /// <summary>Verifies the OTP for that mobile/email and, only if valid, deletes the account.</summary>
        Task<CandidateDeleteAccountResponseDto> ConfirmPublicDeleteAsync(
            CandidatePublicDeleteConfirmRequestDto request,
            string ipAddress);
    }
}

// ────────────────────────────────────────────────────────────
//  Implementation
// ────────────────────────────────────────────────────────────
namespace JobPortal.Services.Implement.Candidate
{
    using JobPortal.Services.IImplement.ICandidate;

    public class CandidateAccountService : ICandidateAccountService
    {
        private const string OtpPurpose = "CandidateAccountDelete";
        private const string PublicOtpPurpose = "CandidateAccountDeletePublic";
        private const int OtpExpiryMinutes = 10;
        private const int ResendCooldownSeconds = 30;
        private const int MaxOtpAttempts = 3;
        private const string DeletedDisplayName = "Deleted User";

        private readonly AppDbContext _context;
        private readonly ITwilioOtpService _twilioOtpService;
        private readonly IEmailService _emailService;
        private readonly ILogger<CandidateAccountService> _logger;

        public CandidateAccountService(
            AppDbContext context,
            ITwilioOtpService twilioOtpService,
            IEmailService emailService,
            ILogger<CandidateAccountService> logger)
        {
            _context = context;
            _twilioOtpService = twilioOtpService;
            _emailService = emailService;
            _logger = logger;
        }

        // ════════════════════════════════════════════════
        // STEP 1 — SEND OTP TO REGISTERED MOBILE
        // ════════════════════════════════════════════════
        public async Task<SendOtpResponseDto> SendDeleteAccountOtpAsync(
            Guid candidateId,
            string ipAddress)
        {
            try
            {
                var (profile, user) = await LoadAsync(candidateId);

                if (profile == null || user == null)
                    return SendFail("Candidate account not found.");

                if (user.IsDeleted)
                    return SendFail("This account has already been deleted.");

                if (string.IsNullOrWhiteSpace(user.MobileNumber) ||
                    string.IsNullOrWhiteSpace(user.CountryCode))
                {
                    return SendFail(
                        "No mobile number is linked to this account, so it cannot be verified by OTP. Please contact support.");
                }

                var mobile = user.MobileNumber.Trim();
                var countryCode = user.CountryCode.Trim();

                // Resend cooldown
                var pending = await _context.OtpVerifications
                    .Where(o =>
                        o.UserId == user.UserId &&
                        o.Purpose == OtpPurpose &&
                        !o.IsVerified)
                    .OrderByDescending(o => o.OtpSentAt)
                    .ToListAsync();

                var recent = pending.FirstOrDefault();

                if (recent != null)
                {
                    var cooldownEnd =
                        recent.OtpSentAt.AddSeconds(recent.ResendCooldownSec);

                    if (DateTime.UtcNow < cooldownEnd)
                    {
                        var waitSecs = Math.Max(
                            1,
                            (int)Math.Ceiling((cooldownEnd - DateTime.UtcNow).TotalSeconds));

                        return SendFail(
                            $"Please wait {waitSecs} seconds before requesting a new OTP.");
                    }

                    _context.OtpVerifications.RemoveRange(pending);
                    await _context.SaveChangesAsync();
                }

                var sent = await _twilioOtpService.SendOtpAsync($"{countryCode}{mobile}");

                if (!sent)
                    return SendFail("Unable to send OTP. Please try again.");

                _context.OtpVerifications.Add(new OtpVerification
                {
                    OtpId = Guid.NewGuid(),
                    UserId = user.UserId,
                    MobileNumber = mobile,
                    CountryCode = countryCode,
                    OtpCode = "TWILIO_VERIFY",
                    OtpSentAt = DateTime.UtcNow,
                    OtpExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes),
                    ResendCooldownSec = ResendCooldownSeconds,
                    OtpAttempts = 0,
                    IsVerified = false,
                    Purpose = OtpPurpose
                });

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Candidate delete-account OTP sent. UserId:{UserId} IP:{IP}",
                    user.UserId, ipAddress);

                var masked = MaskMobile(mobile);

                return new SendOtpResponseDto
                {
                    Success = true,
                    Message = $"OTP sent to {masked}. Valid for {OtpExpiryMinutes} minutes.",
                    MaskedIdentifier = masked,
                    IdentifierType = "mobile",
                    ExpiresInSeconds = OtpExpiryMinutes * 60,
                    ResendCooldownSeconds = ResendCooldownSeconds
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Candidate delete-account SendOtp error. CandidateId:{Id} IP:{IP}",
                    candidateId, ipAddress);

                return SendFail(
                    "We couldn't send the OTP right now. Please try again in a few minutes.");
            }
        }

        // ════════════════════════════════════════════════
        // STEP 2 — VERIFY OTP, THEN DELETE
        // ════════════════════════════════════════════════
        public async Task<CandidateDeleteAccountResponseDto> ConfirmDeleteAccountAsync(
            Guid candidateId,
            CandidateDeleteAccountRequestDto request,
            string? jwtJti,
            DateTime? jwtExpiresAt,
            string ipAddress)
        {
            try
            {
                var (profile, user) = await LoadAsync(candidateId);

                if (profile == null || user == null)
                    return DeleteFail("Candidate account not found.");

                if (user.IsDeleted)
                    return DeleteFail("This account has already been deleted.");

                if (string.IsNullOrWhiteSpace(user.MobileNumber) ||
                    string.IsNullOrWhiteSpace(user.CountryCode))
                {
                    return DeleteFail(
                        "No mobile number is linked to this account, so it cannot be verified by OTP. Please contact support.");
                }

                // ── 1. OTP gate ─────────────────────────────
                var otp = await _context.OtpVerifications
                    .Where(o =>
                        o.UserId == user.UserId &&
                        o.Purpose == OtpPurpose &&
                        !o.IsVerified)
                    .OrderByDescending(o => o.OtpSentAt)
                    .FirstOrDefaultAsync();

                if (otp == null)
                    return DeleteFail("OTP not found. Please request a new OTP.");

                if (DateTime.UtcNow > otp.OtpExpiresAt)
                    return DeleteFail("OTP has expired. Please request a new one.");

                if (otp.OtpAttempts >= MaxOtpAttempts)
                    return DeleteFail("Too many failed attempts. Please request a new OTP.");

                var phoneNumber =
                    $"{user.CountryCode.Trim()}{user.MobileNumber.Trim()}";

                var valid = await _twilioOtpService.VerifyOtpAsync(
                    phoneNumber,
                    request.OtpCode.Trim());

                if (!valid)
                {
                    otp.OtpAttempts++;
                    await _context.SaveChangesAsync();

                    var remaining = MaxOtpAttempts - otp.OtpAttempts;

                    return DeleteFail(
                        remaining > 0
                            ? $"Invalid OTP. {remaining} attempt(s) remaining."
                            : "Too many failed attempts. Please request a new OTP.");
                }

                // ── 2. OTP is valid — delete the account ────
                return await DeleteAccountCoreAsync(
                    profile, user, jwtJti, jwtExpiresAt, ipAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Candidate delete-account confirm error. CandidateId:{Id} IP:{IP}",
                    candidateId, ipAddress);

                return DeleteFail(
                    "We couldn't delete your account right now. Please try again.");
            }
        }

        // ════════════════════════════════════════════════
        // PUBLIC FLOW (NO TOKEN) — mobile/email -> OTP -> delete
        // ════════════════════════════════════════════════

        private const string GenericSendMessage =
            "If an account exists for this mobile number / email, an OTP has been sent.";

        private const string GenericConfirmFail =
            "Invalid or expired OTP.";

        public async Task<SendOtpResponseDto> SendPublicDeleteOtpAsync(
            CandidatePublicDeleteSendOtpRequestDto request,
            string ipAddress)
        {
            try
            {
                if (!TryParseIdentifier(
                        request.Identifier, request.CountryCode,
                        out var identifier, out var isEmail, out var countryCode,
                        out var parseError))
                {
                    return SendFail(parseError);
                }

                var user = await FindCandidateUserAsync(identifier, isEmail, countryCode);

                var genericOk = new SendOtpResponseDto
                {
                    Success = true,
                    Message = GenericSendMessage,
                    MaskedIdentifier = isEmail ? MaskEmail(identifier) : MaskMobile(identifier),
                    IdentifierType = isEmail ? "email" : "mobile",
                    ExpiresInSeconds = OtpExpiryMinutes * 60,
                    ResendCooldownSeconds = ResendCooldownSeconds
                };

                // Same response whether or not the account exists, so this
                // public endpoint can't be used to find out who is registered.
                if (user == null)
                {
                    _logger.LogInformation(
                        "Public delete OTP requested for unknown identifier. IP:{IP}", ipAddress);
                    return genericOk;
                }

                var pending = await _context.OtpVerifications
                    .Where(o =>
                        o.UserId == user.UserId &&
                        o.Purpose == PublicOtpPurpose &&
                        !o.IsVerified)
                    .OrderByDescending(o => o.OtpSentAt)
                    .ToListAsync();

                var recent = pending.FirstOrDefault();

                if (recent != null)
                {
                    var cooldownEnd =
                        recent.OtpSentAt.AddSeconds(recent.ResendCooldownSec);

                    if (DateTime.UtcNow < cooldownEnd)
                    {
                        var waitSecs = Math.Max(
                            1,
                            (int)Math.Ceiling((cooldownEnd - DateTime.UtcNow).TotalSeconds));

                        return SendFail(
                            $"Please wait {waitSecs} seconds before requesting a new OTP.");
                    }

                    _context.OtpVerifications.RemoveRange(pending);
                    await _context.SaveChangesAsync();
                }

                OtpVerification record;

                if (isEmail)
                {
                    var otpCode = GenerateOtp();

                    record = new OtpVerification
                    {
                        OtpId = Guid.NewGuid(),
                        UserId = user.UserId,
                        MobileNumber = identifier,
                        CountryCode = "email",
                        OtpCode = BCrypt.Net.BCrypt.HashPassword(otpCode),
                        OtpSentAt = DateTime.UtcNow,
                        OtpExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes),
                        ResendCooldownSec = ResendCooldownSeconds,
                        OtpAttempts = 0,
                        IsVerified = false,
                        Purpose = PublicOtpPurpose
                    };

                    _context.OtpVerifications.Add(record);
                    await _context.SaveChangesAsync();

                    await _emailService.SendOtpEmailAsync(identifier, otpCode);
                }
                else
                {
                    // Always use the number stored on the account.
                    var sent = await _twilioOtpService.SendOtpAsync(
                        $"{user.CountryCode!.Trim()}{user.MobileNumber!.Trim()}");

                    if (!sent)
                        return SendFail("Unable to send OTP. Please try again.");

                    record = new OtpVerification
                    {
                        OtpId = Guid.NewGuid(),
                        UserId = user.UserId,
                        MobileNumber = identifier,
                        CountryCode = user.CountryCode!.Trim(),
                        OtpCode = "TWILIO_VERIFY",
                        OtpSentAt = DateTime.UtcNow,
                        OtpExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes),
                        ResendCooldownSec = ResendCooldownSeconds,
                        OtpAttempts = 0,
                        IsVerified = false,
                        Purpose = PublicOtpPurpose
                    };

                    _context.OtpVerifications.Add(record);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation(
                    "Public delete-account OTP sent. UserId:{UserId} Channel:{Channel} IP:{IP}",
                    user.UserId, isEmail ? "email" : "mobile", ipAddress);

                return genericOk;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Public delete-account SendOtp error. IP:{IP}", ipAddress);

                return SendFail(
                    "We couldn't send the OTP right now. Please try again in a few minutes.");
            }
        }

        public async Task<CandidateDeleteAccountResponseDto> ConfirmPublicDeleteAsync(
            CandidatePublicDeleteConfirmRequestDto request,
            string ipAddress)
        {
            try
            {
                if (!TryParseIdentifier(
                        request.Identifier, request.CountryCode,
                        out var identifier, out var isEmail, out var countryCode,
                        out var parseError))
                {
                    return DeleteFail(parseError);
                }

                var user = await FindCandidateUserAsync(identifier, isEmail, countryCode);

                if (user == null)
                    return DeleteFail(GenericConfirmFail);

                var otp = await _context.OtpVerifications
                    .Where(o =>
                        o.UserId == user.UserId &&
                        o.Purpose == PublicOtpPurpose &&
                        !o.IsVerified)
                    .OrderByDescending(o => o.OtpSentAt)
                    .FirstOrDefaultAsync();

                if (otp == null)
                    return DeleteFail("OTP not found. Please request a new OTP.");

                // The OTP must be checked on the same channel it was sent on.
                var otpWasEmail = otp.CountryCode == "email";
                if (otpWasEmail != isEmail)
                    return DeleteFail(GenericConfirmFail);

                if (DateTime.UtcNow > otp.OtpExpiresAt)
                    return DeleteFail("OTP has expired. Please request a new one.");

                if (otp.OtpAttempts >= MaxOtpAttempts)
                    return DeleteFail("Too many failed attempts. Please request a new OTP.");

                var code = request.OtpCode.Trim();

                var valid = isEmail
                    ? BCrypt.Net.BCrypt.Verify(code, otp.OtpCode)
                    : await _twilioOtpService.VerifyOtpAsync(
                        $"{user.CountryCode!.Trim()}{user.MobileNumber!.Trim()}",
                        code);

                if (!valid)
                {
                    otp.OtpAttempts++;
                    await _context.SaveChangesAsync();

                    var remaining = MaxOtpAttempts - otp.OtpAttempts;

                    return DeleteFail(
                        remaining > 0
                            ? $"Invalid OTP. {remaining} attempt(s) remaining."
                            : "Too many failed attempts. Please request a new OTP.");
                }

                var profile = await _context.CandidateProfiles
                    .FirstOrDefaultAsync(p => p.UserId == user.UserId);

                return await DeleteAccountCoreAsync(profile, user, null, null, ipAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Public delete-account confirm error. IP:{IP}", ipAddress);

                return DeleteFail(
                    "We couldn't delete your account right now. Please try again.");
            }
        }

        /// <summary>Finds a live (not deleted) candidate account by email or mobile.</summary>
        private async Task<User?> FindCandidateUserAsync(
            string identifier, bool isEmail, string? countryCode)
        {
            if (isEmail)
            {
                return await _context.Users.FirstOrDefaultAsync(u =>
                    u.UserType == UserType.Candidate &&
                    !u.IsDeleted &&
                    u.Email != null &&
                    u.Email.ToLower() == identifier);
            }

            return await _context.Users.FirstOrDefaultAsync(u =>
                u.UserType == UserType.Candidate &&
                !u.IsDeleted &&
                u.MobileNumber == identifier &&
                u.CountryCode == countryCode);
        }

        private static bool TryParseIdentifier(
            string raw,
            string? rawCountryCode,
            out string identifier,
            out bool isEmail,
            out string? countryCode,
            out string error)
        {
            identifier = (raw ?? string.Empty).Trim().ToLowerInvariant();
            isEmail = identifier.Contains('@') && identifier.Contains('.');
            countryCode = string.IsNullOrWhiteSpace(rawCountryCode)
                ? null
                : rawCountryCode.Trim();
            error = string.Empty;

            if (isEmail)
                return true;

            identifier = identifier
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("(", "")
                .Replace(")", "");

            if (identifier.Length < 7 || identifier.Length > 12 || !identifier.All(char.IsDigit))
            {
                error = "Please enter a valid email or mobile number.";
                return false;
            }

            if (countryCode == null)
            {
                error = "Country code is required for mobile number.";
                return false;
            }

            return true;
        }

        private static string GenerateOtp()
        {
            var bytes = new byte[4];
            System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
            return (BitConverter.ToUInt32(bytes) % 1_000_000).ToString("D6");
        }

        private static string MaskEmail(string email)
        {
            var parts = email.Split('@');
            var name = parts[0];
            var masked = name.Length <= 2
                ? (name.Length == 0 ? "***" : name[0] + "***")
                : name[..2] + new string('*', name.Length - 2);
            return $"{masked}@{parts[1]}";
        }

        /// <summary>
        /// Shared by the logged-in and the public (no-token) flows. Call ONLY
        /// after the OTP has been verified. Profile may be null for accounts
        /// that never created one (e.g. Google sign-ups).
        /// </summary>
        private async Task<CandidateDeleteAccountResponseDto> DeleteAccountCoreAsync(
            CandidateProfile? profile,
            User user,
            string? jwtJti,
            DateTime? jwtExpiresAt,
            string ipAddress)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();

            await PurgePersonalDataAsync(profile?.CandidateId, user.UserId);

            var now = DateTime.UtcNow;

            // Anonymise the profile. The row itself has to stay because
            // payment transactions / invoices / job applications point
            // at it (Restrict FKs) and must be kept for billing history.
            if (profile != null)
            {
                profile.FullName = DeletedDisplayName;
                profile.ProfilePhotoUrl = null;
                profile.ProfilePhotoPublicId = null;
                profile.Role = null;
                profile.DateOfBirth = null;
                profile.Gender = null;
                profile.Nationality = null;
                profile.CurrentCity = null;
                profile.CurrentState = null;
                profile.Pincode = null;
                profile.PreferredWorkLocation = null;
                profile.DisabilityNote = null;
                profile.ProfessionalSummary = null;
                profile.About = null;
                profile.FcmToken = null;
                profile.GeneratedCvFileUrl = null;
                profile.GeneratedCvPublicId = null;
                profile.CurrentLatitude = null;
                profile.CurrentLongitude = null;
                profile.LocationPermissionGranted = false;
                profile.ProfileStatus = "Deleted";
                profile.UpdatedAt = now;
            }

            // Soft-delete the login account. AccountStatus.Suspended is
            // what the candidate login (OTP / Google / LinkedIn) already
            // refuses, so the account can no longer sign in.
            user.IsDeleted = true;
            user.DeletedAt = now;
            user.DeletedByUserId = user.UserId;
            user.RecoveryExpiry = null;
            user.AccountStatus = AccountStatus.Suspended;
            user.SuspensionReason = "Account deleted by candidate.";
            user.UpdatedAt = now;

            // Record the current JWT as logged out (same record the normal
            // logout endpoint writes). Only for the logged-in flow.
            if (profile != null && !string.IsNullOrWhiteSpace(jwtJti))
            {
                _context.CandidateLogoutSessions.Add(new CandidateLogoutSession
                {
                    LogoutSessionId = Guid.NewGuid(),
                    CandidateId = profile.CandidateId,
                    JwtJti = jwtJti,
                    LoggedOutAt = now,
                    JwtExpiresAt = jwtExpiresAt
                });
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            _logger.LogInformation(
                "Candidate account deleted. UserId:{UserId} CandidateId:{CandidateId} IP:{IP}",
                user.UserId, profile?.CandidateId, ipAddress);

            return new CandidateDeleteAccountResponseDto
            {
                Success = true,
                Message = "Your account has been deleted successfully."
            };
        }

        // ════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════

        /// <summary>
        /// Removes everything personal that nobody else needs: documents,
        /// education, work history, skills, saved jobs, AI embedding,
        /// push tokens, notifications, sessions and OTP rows, and blanks
        /// the CV file links / parsed CV data. Runs inside the caller's
        /// transaction.
        /// </summary>
        private async Task PurgePersonalDataAsync(Guid? candidateIdOrNull, Guid userId)
        {
            // Guid.Empty matches no rows when the account has no profile.
            var candidateId = candidateIdOrNull ?? Guid.Empty;

            await _context.CandidateDeviceTokens
                .Where(x => x.CandidateId == candidateId)
                .ExecuteDeleteAsync();

            await _context.CandidateDocuments
                .Where(x => x.CandidateId == candidateId)
                .ExecuteDeleteAsync();

            await _context.CandidateEducations
                .Where(x => x.CandidateId == candidateId)
                .ExecuteDeleteAsync();

            await _context.CandidateWorkHistories
                .Where(x => x.CandidateId == candidateId)
                .ExecuteDeleteAsync();

            await _context.CandidateSkills
                .Where(x => x.CandidateId == candidateId)
                .ExecuteDeleteAsync();

            await _context.CandidateEmbeddings
                .Where(x => x.CandidateId == candidateId)
                .ExecuteDeleteAsync();

            await _context.SavedJobs
                .Where(x => x.CandidateId == candidateId)
                .ExecuteDeleteAsync();

            // CV rows are referenced by employer CV-download history, so
            // the rows stay but the files / parsed personal data are wiped.
            await _context.CandidateCvs
                .Where(x => x.CandidateId == candidateId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.CvFileUrl, (string?)null)
                    .SetProperty(x => x.CvPublicId, (string?)null)
                    .SetProperty(x => x.CvPdfUrl, (string?)null)
                    .SetProperty(x => x.ParsedName, (string?)null)
                    .SetProperty(x => x.ParsedEmail, (string?)null)
                    .SetProperty(x => x.ParsedPhone, (string?)null)
                    .SetProperty(x => x.ParsedSummary, (string?)null)
                    .SetProperty(x => x.ParsedCity, (string?)null)
                    .SetProperty(x => x.ParsedState, (string?)null)
                    .SetProperty(x => x.ParsedCountry, (string?)null)
                    .SetProperty(x => x.ParsedSkillsJson, (string?)null)
                    .SetProperty(x => x.ParsedEducationJson, (string?)null)
                    .SetProperty(x => x.ParsedWorkHistoryJson, (string?)null)
                    .SetProperty(x => x.ParsedLanguagesJson, (string?)null)
                    .SetProperty(x => x.ParsedCertificatesJson, (string?)null)
                    .SetProperty(x => x.ParsedProjectsJson, (string?)null)
                    .SetProperty(x => x.ParsedRawJson, (string?)null));

            await _context.Notifications
                .Where(x => x.UserId == userId)
                .ExecuteDeleteAsync();

            await _context.UserSessions
                .Where(x => x.UserId == userId && !x.IsRevoked)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRevoked, true));

            // Includes the OTP that was just verified.
            await _context.OtpVerifications
                .Where(x => x.UserId == userId)
                .ExecuteDeleteAsync();
        }

        private async Task<(CandidateProfile? profile, User? user)> LoadAsync(Guid candidateId)
        {
            var profile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile == null)
                return (null, null);

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserId == profile.UserId &&
                    u.UserType == UserType.Candidate);

            return (profile, user);
        }

        private static string MaskMobile(string mobile) =>
            mobile.Length <= 4
                ? "****"
                : new string('*', mobile.Length - 4) + mobile[^4..];

        private static SendOtpResponseDto SendFail(string message) =>
            new() { Success = false, Message = message };

        private static CandidateDeleteAccountResponseDto DeleteFail(string message) =>
            new() { Success = false, Message = message };
    }
}