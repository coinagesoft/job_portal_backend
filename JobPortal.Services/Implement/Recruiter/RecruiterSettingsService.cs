using JobPortal.Application.DTOs.Recruiter.Settings;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums.common;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace JobPortal.Services.Implement.Recruiter
{
    public class RecruiterSettingsService : IRecruiterSettingsService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RecruiterSettingsService> _logger;

        public RecruiterSettingsService(
            AppDbContext context,
            ILogger<RecruiterSettingsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Account Settings

        public async Task<GetAccountSettingsResponseDto?> GetAccountSettingsAsync(
            Guid employerId)
        {
            var employer = await _context.EmployerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EmployerId == employerId);

            if (employer == null)
                return null;

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == employer.UserId);

            if (user == null)
                return null;

            return new GetAccountSettingsResponseDto
            {
                EmployerId = employer.EmployerId,
                ContactPersonName = employer.ContactPersonName,
                Designation = employer.Designation,
                Email = user.Email ?? string.Empty,
                MobileNumber = user.MobileNumber,
                CountryCode = user.CountryCode,
                TimeZone = employer.TimeZone
            };
        }

        public async Task<UpdateAccountSettingsResponseDto> UpdateAccountSettingsAsync(
    Guid employerId,
    UpdateAccountSettingsRequestDto request)
        {
            var employer = await _context.EmployerProfiles
                .FirstOrDefaultAsync(x => x.EmployerId == employerId);

            if (employer == null)
            {
                return new UpdateAccountSettingsResponseDto
                {
                    Success = false,
                    Message = "Employer not found."
                };
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.UserId == employer.UserId);

            if (user == null)
            {
                return new UpdateAccountSettingsResponseDto
                {
                    Success = false,
                    Message = "User not found."
                };
            }

            // Email changes now require the OTP flow (see
            // RequestEmailChangeOtpAsync / VerifyEmailChangeOtpAsync) —
            // this PATCH silently ignoring a mismatched value used to be a
            // security gap: OtpRequired/VerificationType existed on the
            // response DTO but were never actually enforced.
            if (!string.IsNullOrWhiteSpace(request.Email) &&
                !string.Equals(request.Email, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                return new UpdateAccountSettingsResponseDto
                {
                    Success = false,
                    Message = "Email changes require OTP verification. Use the \"Change Email\" flow instead.",
                    OtpRequired = true,
                    VerificationType = "Email"
                };
            }

            // Mobile changes now require the OTP flow (see
            // RequestMobileChangeOtpAsync / VerifyMobileChangeOtpAsync).
            if (!string.IsNullOrWhiteSpace(request.MobileNumber) &&
                request.MobileNumber != user.MobileNumber)
            {
                return new UpdateAccountSettingsResponseDto
                {
                    Success = false,
                    Message = "Mobile number changes require OTP verification. Use the \"Change Mobile Number\" flow instead.",
                    OtpRequired = true,
                    VerificationType = "Mobile"
                };
            }

            // PATCH logic — identity/contact fields only. Email and
            // MobileNumber/CountryCode are deliberately excluded here.

            if (request.ContactPersonName != null)
                employer.ContactPersonName = request.ContactPersonName;

            if (request.Designation != null)
                employer.Designation = request.Designation;

            if (request.TimeZone != null)
                employer.TimeZone = request.TimeZone;

            employer.UpdatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new UpdateAccountSettingsResponseDto
            {
                Success = true,
                Message = "Account settings updated successfully.",
                OtpRequired = false
            };
        }

        #endregion

        #region Account Email / Mobile Change (OTP-gated)

        private const int OtpExpiryMinutes = 10;
        private const int OtpResendCooldownSec = 60;

        private static string GenerateOtp()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        /// <summary>
        /// Step 1 of changing the account email: validates the new address
        /// is free, invalidates any earlier pending OTP for it, and issues
        /// a fresh one. Real sending is QA-bypassed for now (static "123456"
        /// accepted in VerifyEmailChangeOtpAsync), matching the exact
        /// convention already used by RecruiterRegistrationService — swap
        /// both back in together once email/SMS sending is wired live.
        /// </summary>
        public async Task<SettingsOtpResponseDto> RequestEmailChangeOtpAsync(
            Guid employerId,
            RequestEmailChangeOtpRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.NewEmail))
            {
                return new SettingsOtpResponseDto
                {
                    Success = false,
                    Message = "A new email address is required."
                };
            }

            var employer = await _context.EmployerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EmployerId == employerId);

            if (employer == null)
            {
                return new SettingsOtpResponseDto
                {
                    Success = false,
                    Message = "Employer not found."
                };
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == employer.UserId);

            if (user == null)
            {
                return new SettingsOtpResponseDto
                {
                    Success = false,
                    Message = "User not found."
                };
            }

            if (string.Equals(request.NewEmail, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                return new SettingsOtpResponseDto
                {
                    Success = false,
                    Message = "This is already your current email address."
                };
            }

            var emailTaken = await _context.Users.AnyAsync(x =>
                x.UserId != user.UserId &&
                x.Email != null &&
                x.Email.ToLower() == request.NewEmail.ToLower());

            if (emailTaken)
            {
                return new SettingsOtpResponseDto
                {
                    Success = false,
                    Message = "This email is already registered to another account."
                };
            }

            var otp = GenerateOtp();

            // ===== QA BYPASS: real email OTP send disabled =====
            // await _emailService.SendOtpEmailAsync(request.NewEmail, otp);
            // ===== END QA BYPASS =====

            var oldOtps = await _context.OtpVerifications
                .Where(x =>
                    x.Email == request.NewEmail &&
                    x.Purpose == "EmployerAccountEmailChange" &&
                    !x.IsVerified)
                .ToListAsync();

            foreach (var item in oldOtps)
                item.IsVerified = true;

            _context.OtpVerifications.Add(new OtpVerification
            {
                OtpId = Guid.NewGuid(),
                UserId = user.UserId,
                Email = request.NewEmail,
                OtpCode = BCrypt.Net.BCrypt.HashPassword(otp),
                OtpSentAt = DateTime.UtcNow,
                OtpExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes),
                Purpose = "EmployerAccountEmailChange",
                IsVerified = false,
                OtpAttempts = 0,
                ResendCooldownSec = OtpResendCooldownSec
            });

            await _context.SaveChangesAsync();

            return new SettingsOtpResponseDto
            {
                Success = true,
                Message = $"An OTP has been sent to {request.NewEmail}.",
                OtpExpiresInSeconds = OtpExpiryMinutes * 60
            };
        }

        /// <summary>
        /// Step 2: verifies the OTP and, only on success, actually applies
        /// the new email to the User row.
        /// </summary>
        public async Task<SettingsOtpResponseDto> VerifyEmailChangeOtpAsync(
            Guid employerId,
            VerifyEmailChangeOtpRequestDto request)
        {
            var employer = await _context.EmployerProfiles
                .FirstOrDefaultAsync(x => x.EmployerId == employerId);

            if (employer == null)
            {
                return new SettingsOtpResponseDto
                {
                    Success = false,
                    Message = "Employer not found."
                };
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.UserId == employer.UserId);

            if (user == null)
            {
                return new SettingsOtpResponseDto
                {
                    Success = false,
                    Message = "User not found."
                };
            }

            var otpRecord = await _context.OtpVerifications
                .Where(x =>
                    x.Email == request.NewEmail &&
                    x.Purpose == "EmployerAccountEmailChange" &&
                    !x.IsVerified)
                .OrderByDescending(x => x.OtpSentAt)
                .FirstOrDefaultAsync();

            if (otpRecord == null)
            {
                return new SettingsOtpResponseDto
                {
                    Success = false,
                    Message = "OTP not found or already used. Please request a new one."
                };
            }

            if (otpRecord.OtpExpiresAt < DateTime.UtcNow)
            {
                return new SettingsOtpResponseDto
                {
                    Success = false,
                    Message = "This OTP has expired. Please request a new one."
                };
            }

            // ===== QA BYPASS: static OTP "123456" accepted, real check disabled =====
            // var valid = BCrypt.Net.BCrypt.Verify(request.OtpCode, otpRecord.OtpCode);
            var valid = request.OtpCode == "123456";
            // ===== END QA BYPASS =====

            if (!valid)
            {
                otpRecord.OtpAttempts++;
                await _context.SaveChangesAsync();

                return new SettingsOtpResponseDto
                {
                    Success = false,
                    Message = "Invalid OTP."
                };
            }

            // Re-check uniqueness right before committing — the address
            // could theoretically have been taken by someone else in the
            // window between requesting and verifying the OTP.
            var emailTaken = await _context.Users.AnyAsync(x =>
                x.UserId != user.UserId &&
                x.Email != null &&
                x.Email.ToLower() == request.NewEmail.ToLower());

            if (emailTaken)
            {
                return new SettingsOtpResponseDto
                {
                    Success = false,
                    Message = "This email is already registered to another account."
                };
            }

            otpRecord.IsVerified = true;
            user.Email = request.NewEmail;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new SettingsOtpResponseDto
            {
                Success = true,
                Message = "Email updated successfully."
            };
        }

        /// <summary>
        /// Step 1 of changing the mobile number — same shape as the email
        /// flow above.
        /// </summary>
        public async Task<SettingsOtpResponseDto> RequestMobileChangeOtpAsync(
            Guid employerId,
            RequestMobileChangeOtpRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.NewMobileNumber) ||
                string.IsNullOrWhiteSpace(request.NewCountryCode))
            {
                return new SettingsOtpResponseDto
                {
                    Success = false,
                    Message = "A new mobile number and country code are required."
                };
            }

            var employer = await _context.EmployerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EmployerId == employerId);

            if (employer == null)
            {
                return new SettingsOtpResponseDto
                {
                    Success = false,
                    Message = "Employer not found."
                };
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == employer.UserId);

            if (user == null)
            {
                return new SettingsOtpResponseDto
                {
                    Success = false,
                    Message = "User not found."
                };
            }

            if (request.NewMobileNumber == user.MobileNumber &&
                request.NewCountryCode == user.CountryCode)
            {
                return new SettingsOtpResponseDto
                {
                    Success = false,
                    Message = "This is already your current mobile number."
                };
            }

            var mobileTaken = await _context.Users.AnyAsync(x =>
                x.UserId != user.UserId &&
                x.MobileNumber == request.NewMobileNumber);

            if (mobileTaken)
            {
                return new SettingsOtpResponseDto
                {
                    Success = false,
                    Message = "This mobile number is already registered to another account."
                };
            }

            var fullPhone = $"{request.NewCountryCode}{request.NewMobileNumber}";
            var otp = GenerateOtp();

            // ===== QA BYPASS: real Twilio OTP send disabled =====
            // await _twilioOtpService.SendOtpAsync(fullPhone);
            // ===== END QA BYPASS =====

            var oldOtps = await _context.OtpVerifications
                .Where(x =>
                    x.MobileNumber == request.NewMobileNumber &&
                    x.Purpose == "EmployerAccountMobileChange" &&
                    !x.IsVerified)
                .ToListAsync();

            foreach (var item in oldOtps)
                item.IsVerified = true;

            _context.OtpVerifications.Add(new OtpVerification
            {
                OtpId = Guid.NewGuid(),
                UserId = user.UserId,
                MobileNumber = request.NewMobileNumber,
                CountryCode = request.NewCountryCode,
                OtpCode = BCrypt.Net.BCrypt.HashPassword(otp),
                OtpSentAt = DateTime.UtcNow,
                OtpExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes),
                Purpose = "EmployerAccountMobileChange",
                IsVerified = false,
                OtpAttempts = 0,
                ResendCooldownSec = OtpResendCooldownSec
            });

            await _context.SaveChangesAsync();

            return new SettingsOtpResponseDto
            {
                Success = true,
                Message = $"An OTP has been sent to {fullPhone}.",
                OtpExpiresInSeconds = OtpExpiryMinutes * 60
            };
        }

        /// <summary>
        /// Step 2: verifies the mobile OTP and, only on success, applies
        /// the new mobile number + country code to the User row.
        /// </summary>
        public async Task<SettingsOtpResponseDto> VerifyMobileChangeOtpAsync(
            Guid employerId,
            VerifyMobileChangeOtpRequestDto request)
        {
            var employer = await _context.EmployerProfiles
                .FirstOrDefaultAsync(x => x.EmployerId == employerId);

            if (employer == null)
            {
                return new SettingsOtpResponseDto
                {
                    Success = false,
                    Message = "Employer not found."
                };
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.UserId == employer.UserId);

            if (user == null)
            {
                return new SettingsOtpResponseDto
                {
                    Success = false,
                    Message = "User not found."
                };
            }

            var otpRecord = await _context.OtpVerifications
                .Where(x =>
                    x.MobileNumber == request.NewMobileNumber &&
                    x.Purpose == "EmployerAccountMobileChange" &&
                    !x.IsVerified)
                .OrderByDescending(x => x.OtpSentAt)
                .FirstOrDefaultAsync();

            if (otpRecord == null)
            {
                return new SettingsOtpResponseDto
                {
                    Success = false,
                    Message = "OTP not found or already used. Please request a new one."
                };
            }

            if (otpRecord.OtpExpiresAt < DateTime.UtcNow)
            {
                return new SettingsOtpResponseDto
                {
                    Success = false,
                    Message = "This OTP has expired. Please request a new one."
                };
            }

            // ===== QA BYPASS: static OTP "123456" accepted, real Twilio check disabled =====
            // var valid = await _twilioOtpService.VerifyOtpAsync(
            //     $"{request.NewCountryCode}{request.NewMobileNumber}", request.OtpCode);
            var valid = request.OtpCode == "123456";
            // ===== END QA BYPASS =====

            if (!valid)
            {
                otpRecord.OtpAttempts++;
                await _context.SaveChangesAsync();

                return new SettingsOtpResponseDto
                {
                    Success = false,
                    Message = "Invalid OTP."
                };
            }

            var mobileTaken = await _context.Users.AnyAsync(x =>
                x.UserId != user.UserId &&
                x.MobileNumber == request.NewMobileNumber);

            if (mobileTaken)
            {
                return new SettingsOtpResponseDto
                {
                    Success = false,
                    Message = "This mobile number is already registered to another account."
                };
            }

            otpRecord.IsVerified = true;
            user.MobileNumber = request.NewMobileNumber;
            user.CountryCode = request.NewCountryCode;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new SettingsOtpResponseDto
            {
                Success = true,
                Message = "Mobile number updated successfully."
            };
        }

        #endregion

        #region Notification Settings

        public async Task<GetNotificationSettingsResponseDto?> GetNotificationSettingsAsync(
                Guid employerId)
        {
            var settings = await _context.EmployerNotificationSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EmployerId == employerId);

            if (settings == null)
                return null;

            return new GetNotificationSettingsResponseDto
            {
                PrefEmailEnabled = settings.PrefEmailEnabled,
                PrefPushEnabled = settings.PrefPushEnabled,
                PrefApplicantNotify = settings.PrefApplicantNotify,
                PrefCreditExpiryEmail = settings.PrefCreditExpiryEmail,
                PrefJobStatusUpdates = settings.PrefJobStatusUpdates,
                PrefSystemMessages = settings.PrefSystemMessages,
                SessionTimeoutMinutes = settings.SessionTimeoutMinutes
            };
        }

        public async Task<UpdateNotificationSettingsResponseDto> UpdateNotificationSettingsAsync(
        Guid employerId,
        UpdateNotificationSettingsRequestDto request)
        {
            var settings = await _context.EmployerNotificationSettings
                .FirstOrDefaultAsync(x => x.EmployerId == employerId);

            if (settings == null)
            {
                settings = new EmployerNotificationSetting
                {
                    NotifPrefId = Guid.NewGuid(),
                    EmployerId = employerId,

                    PrefEmailEnabled = true,
                    PrefPushEnabled = true,
                    PrefApplicantNotify = true,
                    PrefCreditExpiryEmail = true,
                    PrefJobStatusUpdates = true,
                    PrefSystemMessages = true,
                    SessionTimeoutMinutes = 30
                };

                _context.EmployerNotificationSettings.Add(settings);
            }

            // PATCH logic

            if (request.PrefEmailEnabled.HasValue)
                settings.PrefEmailEnabled = request.PrefEmailEnabled.Value;

            if (request.PrefPushEnabled.HasValue)
                settings.PrefPushEnabled = request.PrefPushEnabled.Value;

            if (request.PrefApplicantNotify.HasValue)
                settings.PrefApplicantNotify = request.PrefApplicantNotify.Value;

            if (request.PrefCreditExpiryEmail.HasValue)
                settings.PrefCreditExpiryEmail = request.PrefCreditExpiryEmail.Value;

            if (request.PrefJobStatusUpdates.HasValue)
                settings.PrefJobStatusUpdates = request.PrefJobStatusUpdates.Value;

            if (request.PrefSystemMessages.HasValue)
                settings.PrefSystemMessages = request.PrefSystemMessages.Value;

            if (request.SessionTimeoutMinutes.HasValue)
                settings.SessionTimeoutMinutes = request.SessionTimeoutMinutes.Value;

            await _context.SaveChangesAsync();

            return new UpdateNotificationSettingsResponseDto
            {
                Success = true,
                Message = "Notification settings updated successfully."
            };
        }

        #endregion

        #region Preferences

        public async Task<GetPreferenceSettingsResponseDto?> GetPreferenceSettingsAsync(
                Guid employerId)
        {
            var preference = await _context.EmployerPreferences
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EmployerId == employerId);

            if (preference == null)
                return null;

            return new GetPreferenceSettingsResponseDto
            {
                PrimaryLanguage = preference.PrimaryLanguage,
                SecondaryLanguage = preference.SecondaryLanguage,
                ItemsPerPage = preference.ItemsPerPage,
                DateFormat = preference.DateFormat,
                MarketingEmailsEnabled = preference.MarketingEmailsEnabled,
                PlatformUpdatesEnabled = preference.PlatformUpdatesEnabled
            };
        }

        public async Task<UpdatePreferenceSettingsResponseDto> UpdatePreferenceSettingsAsync(
     Guid employerId,
     UpdatePreferenceSettingsRequestDto request)
        {
            var preference = await _context.EmployerPreferences
                .FirstOrDefaultAsync(x => x.EmployerId == employerId);

            if (preference == null)
            {
                preference = new EmployerPreference
                {
                    PreferenceId = Guid.NewGuid(),
                    EmployerId = employerId,

                    PrimaryLanguage = "English",
                    SecondaryLanguage = "Hindi",
                    ItemsPerPage = 10,
                    DateFormat = "dd/MM/yyyy",
                    MarketingEmailsEnabled = true,
                    PlatformUpdatesEnabled = true,

                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.EmployerPreferences.Add(preference);
            }

            // PATCH logic

            if (request.PrimaryLanguage != null)
                preference.PrimaryLanguage = request.PrimaryLanguage;

            if (request.SecondaryLanguage != null)
                preference.SecondaryLanguage = request.SecondaryLanguage;

            if (request.ItemsPerPage.HasValue)
                preference.ItemsPerPage = request.ItemsPerPage.Value;

            if (request.DateFormat != null)
                preference.DateFormat = request.DateFormat;

            if (request.MarketingEmailsEnabled.HasValue)
                preference.MarketingEmailsEnabled =
                    request.MarketingEmailsEnabled.Value;

            if (request.PlatformUpdatesEnabled.HasValue)
                preference.PlatformUpdatesEnabled =
                    request.PlatformUpdatesEnabled.Value;

            preference.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new UpdatePreferenceSettingsResponseDto
            {
                Success = true,
                Message = "Preferences updated successfully."
            };
        }

        #endregion

        #region Sessions

        public async Task<GetUserSessionsResponseDto?> GetUserSessionsAsync(
                Guid employerId)
        {
            var employer = await _context.EmployerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EmployerId == employerId);

            if (employer == null)
                return null;

            var sessions = await _context.UserSessions
                .AsNoTracking()
                .Where(x =>
                    x.UserId == employer.UserId &&
                    !x.IsRevoked)
                .OrderByDescending(x => x.LastSeenAt)
                .ToListAsync();

            return new GetUserSessionsResponseDto
            {
                TotalSessions = sessions.Count,
                Sessions = sessions.Select(x => new UserSessionDto
                {
                    SessionId = x.SessionId,
                    DeviceName = x.DeviceName,
                    Browser = x.Browser,
                    OperatingSystem = x.OperatingSystem,
                    Location = x.Location,
                    IpAddress = x.IpAddress,
                    IsCurrentSession = x.IsCurrentSession,
                    LastSeenAt = x.LastSeenAt
                }).ToList()
            };
        }

        public async Task<RevokeSessionResponseDto> RevokeSessionAsync(
            Guid sessionId)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(x => x.SessionId == sessionId);

            if (session == null)
            {
                return new RevokeSessionResponseDto
                {
                    Success = false,
                    Message = "Session not found."
                };
            }

            session.IsRevoked = true;

            await _context.SaveChangesAsync();

            return new RevokeSessionResponseDto
            {
                Success = true,
                Message = "Session revoked successfully."
            };
        }

        #endregion

        #region Danger Zone

        /// <summary>
        /// Temporarily disables the employer account. Reversible —
        /// the owner just has to contact support to be reactivated.
        /// Every active session (owner + sub-users) is revoked
        /// immediately so no one keeps working on a stale token.
        /// </summary>
        public async Task<DangerZoneActionResponseDto> DeactivateAccountAsync(
            Guid employerId)
        {
            var employer = await _context.EmployerProfiles
                .FirstOrDefaultAsync(x => x.EmployerId == employerId);

            if (employer == null)
            {
                return new DangerZoneActionResponseDto
                {
                    Success = false,
                    Message = "Employer not found."
                };
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.UserId == employer.UserId);

            if (user == null)
            {
                return new DangerZoneActionResponseDto
                {
                    Success = false,
                    Message = "User not found."
                };
            }

            employer.AccountStatus = AccountStatus.Suspended;
            employer.UpdatedAt = DateTime.UtcNow;

            user.AccountStatus = AccountStatus.Suspended;
            user.SuspensionReason = "Deactivated by employer via Account Settings.";
            user.UpdatedAt = DateTime.UtcNow;

            await RevokeAllSessionsAsync(user.UserId);

            await _context.SaveChangesAsync();

            return new DangerZoneActionResponseDto
            {
                Success = true,
                Message = "Your account has been deactivated. Contact support to reactivate it."
            };
        }

        /// <summary>
        /// Soft-deletes every job posting belonging to the employer.
        /// Applicant records are left untouched for audit purposes —
        /// the jobs themselves are archived and hidden everywhere.
        /// </summary>
        public async Task<DangerZoneActionResponseDto> DeleteAllJobsAsync(
            Guid employerId)
        {
            var employer = await _context.EmployerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EmployerId == employerId);

            if (employer == null)
            {
                return new DangerZoneActionResponseDto
                {
                    Success = false,
                    Message = "Employer not found."
                };
            }

            var affected = await ArchiveAllJobsAsync(employerId);

            await _context.SaveChangesAsync();

            return new DangerZoneActionResponseDto
            {
                Success = true,
                Message = affected > 0
                    ? $"{affected} job posting(s) deleted successfully."
                    : "You had no active jobs to delete.",
                JobsAffected = affected
            };
        }

        /// <summary>
        /// Permanently HARD-deletes the employer account and every row
        /// tied to it — jobs, applications, sub-users (including their
        /// own login accounts), credit/billing/payment history,
        /// invoices, notifications, consent logs, sessions, OTPs — then
        /// the EmployerProfile and User rows themselves.
        ///
        /// This is a real DELETE, not a status flip: once this returns
        /// successfully there is nothing left in the database to
        /// recover. Everything runs inside one transaction so a
        /// failure partway through rolls back cleanly instead of
        /// leaving the account half-deleted.
        ///
        /// Deletes are ordered leaf-to-root to satisfy FK constraints
        /// (several of these — JobPosting.EmployerId, RecruiterNote
        /// .EmployerId, EmployerPlanPurchase.EmployerId,
        /// PaymentTransaction.OriginalTxnId — are configured Restrict,
        /// not Cascade, so parents must not be deleted before their
        /// children).
        /// </summary>
        public async Task<DangerZoneActionResponseDto> DeleteAccountAsync(Guid employerId)
        {
            var employer = await _context.EmployerProfiles
                .FirstOrDefaultAsync(x => x.EmployerId == employerId);

            if (employer == null)
            {
                return new DangerZoneActionResponseDto
                {
                    Success = false,
                    Message = "Employer not found."
                };
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.UserId == employer.UserId);

            if (user == null)
            {
                return new DangerZoneActionResponseDto
                {
                    Success = false,
                    Message = "User not found."
                };
            }

            // Already deleted
            if (user.IsDeleted)
            {
                return new DangerZoneActionResponseDto
                {
                    Success = false,
                    Message = "Account is already scheduled for deletion."
                };
            }

            // Archive all jobs
            var jobsAffected = await ArchiveAllJobsAsync(employerId);

            // Soft delete
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.RecoveryExpiry = DateTime.UtcNow.AddDays(30);

            user.AccountStatus = AccountStatus.Suspended;
            user.SuspensionReason = "Account deleted by employer.";

            user.UpdatedAt = DateTime.UtcNow;

            employer.AccountStatus = AccountStatus.Suspended;
            employer.UpdatedAt = DateTime.UtcNow;

            // Revoke all sessions
            await RevokeAllSessionsAsync(user.UserId);

            await _context.SaveChangesAsync();

            return new DangerZoneActionResponseDto
            {
                Success = true,
                JobsAffected = jobsAffected,
                Message = "Your account has been deleted. You can recover it within 30 days by logging in again."
            };
        }

        // ── Private Helpers ───────────────────────────────────

        private async Task<int> ArchiveAllJobsAsync(Guid employerId)
        {
            var jobs = await _context.JobPostings
                .Where(x => x.EmployerId == employerId && !x.IsDeleted)
                .ToListAsync();

            foreach (var job in jobs)
            {
                job.IsDeleted = true;
                job.IsActive = false;
                job.JobStatus = JobPortal.Domain.Enums.RecruiterEnums.JobStatus.Archived;
                job.UpdatedAt = DateTime.UtcNow;
            }

            return jobs.Count;
        }

        private async Task RevokeAllSessionsAsync(Guid userId)
        {
            var sessions = await _context.UserSessions
                .Where(x => x.UserId == userId && !x.IsRevoked)
                .ToListAsync();

            foreach (var session in sessions)
            {
                session.IsRevoked = true;
            }
        }

        /// <summary>
        /// Reverses DeactivateAccountAsync. Support/admin-triggered only —
        /// a Suspended account is blocked at login, so the employer has
        /// no self-service way back in. Refuses to touch a Deleted
        /// account (that path is one-way).
        /// </summary>
        public async Task<DangerZoneActionResponseDto> ReactivateAccountAsync(
            Guid employerId)
        {
            var employer = await _context.EmployerProfiles
                .FirstOrDefaultAsync(x => x.EmployerId == employerId);

            if (employer == null)
            {
                return new DangerZoneActionResponseDto
                {
                    Success = false,
                    Message = "Employer not found."
                };
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.UserId == employer.UserId);

            if (user == null)
            {
                return new DangerZoneActionResponseDto
                {
                    Success = false,
                    Message = "User not found."
                };
            }

            if (employer.AccountStatus == AccountStatus.Deleted ||
                user.AccountStatus == AccountStatus.Deleted)
            {
                return new DangerZoneActionResponseDto
                {
                    Success = false,
                    Message = "This account was permanently deleted and cannot be reactivated."
                };
            }

            employer.AccountStatus = AccountStatus.Active;
            employer.UpdatedAt = DateTime.UtcNow;

            user.AccountStatus = AccountStatus.Active;
            user.SuspensionReason = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new DangerZoneActionResponseDto
            {
                Success = true,
                Message = "Account reactivated successfully. The employer can log in again."
            };
        }

        #endregion
    }
}