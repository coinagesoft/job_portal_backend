using JobPortal.Application.DTOs.Recruiter;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums;
using JobPortal.Domain.Enums.common;
using JobPortal.Domain.Enums.Common;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Recruiter;

public class RecruiterRegistrationService : IRecruiterRegistrationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<RecruiterRegistrationService> _logger;

    public RecruiterRegistrationService(
        AppDbContext context,
        ILogger<RecruiterRegistrationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ════════════════════════════════════════════════
    // STEP 1 — GST Check → save to DB immediately
    // ════════════════════════════════════════════════
    public async Task<GstCheckResponseDto> CheckGstAsync(
        GstCheckRequestDto request, string ipAddress)
    {
        try
        {
            // ── Create session in DB right away ────────────
            var session = new RegistrationSession
            {
                SessionId = Guid.NewGuid(),
                SessionType = "Recruiter",
                GstRegistered = request.GstRegistered,
                RequiresSecurityDeposit = false,
                IndustryType = request.IndustryType.ToString(),
                CurrentStep = 1,
                LastCompletedStep = 1
               
            };

            _context.RegistrationSessions.Add(session);
            await _context.SaveChangesAsync();         // ✅ saved to DB immediately

            _logger.LogInformation(
                "Step1 saved — Session:{Id} GST:{Gst} Industry:{Ind} IP:{IP}",
                session.SessionId, request.GstRegistered,
                request.IndustryType, ipAddress);

            return new GstCheckResponseDto
            {
                Success = true,
                Message = request.GstRegistered
        ? "GST registered. Proceed to company details."
        : "Non-GST entity. Proceed to company details.",
                GstRegistered = request.GstRegistered,
                IndustryType = request.IndustryType.ToString(),
                RegistrationSessionId = session.SessionId.ToString(),
                StepStatus = BuildStepStatus(session)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GST check error. IP:{IP}", ipAddress);
            return new GstCheckResponseDto
            {
                Success = false,
                Message = ex.InnerException?.InnerException?.Message
                       ?? ex.InnerException?.Message
                       ?? ex.Message
            };
        }
    }

    // ════════════════════════════════════════════════
    // STEP 2 — Company Details → update DB immediately
    // ════════════════════════════════════════════════
    public async Task<CompanyDetailsResponseDto> SaveCompanyDetailsAsync(
        CompanyDetailsRequestDto request, string sessionId)
    {
        try
        {
            var session = await GetValidSessionAsync(sessionId);
            if (session == null)
                return new CompanyDetailsResponseDto
                {
                    Success = false,
                    Message = "Session expired. Please start again."
                };

            // ── Validate step order ────────────────────────
            if (session.LastCompletedStep < 1)
                return new CompanyDetailsResponseDto
                {
                    Success = false,
                    Message = "Please complete Step 1 (GST Check) first."
                };

            // ── Handle logo upload ─────────────────────────
            string? logoUrl = null;
            if (request.CompanyLogo != null && request.CompanyLogo.Length > 0)
            {
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png" };
                if (!allowedTypes.Contains(request.CompanyLogo.ContentType))
                    return new CompanyDetailsResponseDto
                    {
                        Success = false,
                        Message = "Logo must be PNG or JPG."
                    };

                if (request.CompanyLogo.Length > 2 * 1024 * 1024)
                    return new CompanyDetailsResponseDto
                    {
                        Success = false,
                        Message = "Logo must be under 2MB."
                    };

                // TODO: Upload to S3
                logoUrl = $"https://s3.amazonaws.com/skillbridge/logos/{Guid.NewGuid()}.jpg";
            }

            // ── Update session in DB ───────────────────────
            session.LegalName = request.LegalName;
            session.TradeName = request.TradeName;
            session.CompanyDisplayName = request.CompanyDisplayName;
            session.BusinessType = request.BusinessType.ToString();
            session.CompanySize = request.CompanySize?.ToString();
            session.Cin = request.Cin;
            session.State = request.State;
            session.City = request.City;
            session.Pincode = request.Pincode;
            session.AddressLine1 = request.AddressLine1;
            session.AddressLine2 = request.AddressLine2;
            session.WebsiteUrl = request.WebsiteUrl;
            session.CompanyLogoUrl = logoUrl;

            // ── NEW ───────────────────────────────────────────
            session.Gstn = request.Gstn;
            session.Pan = request.Pan;
            session.GstnRegistrationDate = request.GstnRegistrationDate;
            // Override IndustryType from Step 2 if provided
            if (request.IndustryType.HasValue)
                session.IndustryType = request.IndustryType.ToString();
            // ─────────────────────────────────────────────────

            session.CurrentStep = 2;
            session.LastCompletedStep = Math.Max(session.LastCompletedStep, 2);

            await _context.SaveChangesAsync();         // ✅ saved to DB immediately

            _logger.LogInformation(
                "Step2 saved — Session:{Id}", session.SessionId);

            return new CompanyDetailsResponseDto
            {
                Success = true,
                Message = "Company details saved.",
                CompanyLogoUrl = logoUrl,
                StepStatus = BuildStepStatus(session)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save company details error.");
            return new CompanyDetailsResponseDto
            {
                Success = false,
                Message = ex.InnerException?.InnerException?.Message
                       ?? ex.InnerException?.Message
                       ?? ex.Message
            };
        }
    }

    // ════════════════════════════════════════════════
    // STEP 3A — Contact + Send OTP → update DB
    // ════════════════════════════════════════════════
    public async Task<ContactDetailsResponseDto> SaveContactAndSendOtpAsync(
        ContactDetailsRequestDto request, string sessionId)
    {
        try
        {
            var session = await GetValidSessionAsync(sessionId);
            if (session == null)
                return new ContactDetailsResponseDto
                {
                    Success = false,
                    Message = "Session expired. Please start again."
                };

            if (session.LastCompletedStep < 2)
                return new ContactDetailsResponseDto
                {
                    Success = false,
                    Message = "Please complete Step 2 (Company Details) first."
                };

            // ── Check mobile not already registered ────────
            var mobileExists = await _context.Users
                .AnyAsync(u =>
                    u.MobileNumber == request.MobileNumber &&
                    u.CountryCode == request.CountryCode &&
                    u.UserType == UserType.Recruiter);

            if (mobileExists)
                return new ContactDetailsResponseDto
                {
                    Success = false,
                    Message = "This mobile number is already registered."
                };

            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == request.CompanyEmail);

            if (emailExists)
                return new ContactDetailsResponseDto
                {
                    Success = false,
                    Message = "This email is already registered."
                };

            // ── Generate and save OTP ──────────────────────
            var otpCode = GenerateOtp();

            var oldOtps = await _context.OtpVerifications
                .Where(o =>
                    o.MobileNumber == request.MobileNumber &&
                    o.CountryCode == request.CountryCode &&
                    o.Purpose == "RecruiterRegistration" &&
                    !o.IsVerified)
                .ToListAsync();

            foreach (var old in oldOtps)
                old.IsVerified = true;

            var otpRecord = new OtpVerification
            {
                OtpId = Guid.NewGuid(),
                MobileNumber = request.MobileNumber,
                CountryCode = request.CountryCode,
                OtpCode = BCrypt.Net.BCrypt.HashPassword(otpCode),
                OtpSentAt = DateTime.UtcNow,
                OtpExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsVerified = false,
                Purpose = "RecruiterRegistration",
                ResendCooldownSec = 60,   // ← add this
                OtpAttempts = 0
            };
            _context.OtpVerifications.Add(otpRecord);

            // ── Update session in DB ───────────────────────
            session.ContactPersonName = request.ContactPersonName;
            session.Designation = request.Designation;
            session.ContactPersonEmail = request.ContactPersonEmail;
            session.CompanyEmail = request.CompanyEmail;
            session.MobileNumber = request.MobileNumber;
            session.CountryCode = request.CountryCode;
            session.CompanyDescription = request.CompanyDescription;
            session.MobileVerified = false;            // reset if resending
            session.CurrentStep = 3;

            await _context.SaveChangesAsync();         // ✅ saved to DB immediately

            _logger.LogInformation(
                "Step3A saved — OTP:{OTP} Session:{Id} [DEV ONLY]",
                otpCode, session.SessionId);

            return new ContactDetailsResponseDto
            {
                Success = true,
                Message = $"OTP sent to {MaskMobile(request.MobileNumber)}. Valid for 10 minutes.",
                MaskedMobile = MaskMobile(request.MobileNumber),
                OtpExpiresInSeconds = 600,
                StepStatus = BuildStepStatus(session)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save contact error.");
            return new ContactDetailsResponseDto
            {
                Success = false,
                Message = ex.InnerException?.InnerException?.Message
                       ?? ex.InnerException?.Message
                       ?? ex.Message
            };
        }
    }

    // ════════════════════════════════════════════════
    // STEP 3B — Verify OTP → update DB
    // ════════════════════════════════════════════════
    public async Task<VerifyContactOtpResponseDto> VerifyContactOtpAsync(
        VerifyContactOtpRequestDto request, string sessionId)
    {
        try
        {
            var session = await GetValidSessionAsync(sessionId);
            if (session == null)
                return new VerifyContactOtpResponseDto
                {
                    Success = false,
                    Message = "Session expired. Please start again."
                };

            var otp = await _context.OtpVerifications
                .Where(o =>
                    o.MobileNumber == request.MobileNumber &&
                    o.CountryCode == request.CountryCode &&
                    o.Purpose == "RecruiterRegistration" &&
                    !o.IsVerified)
                .OrderByDescending(o => o.OtpSentAt)
                .FirstOrDefaultAsync();

            if (otp == null)
                return new VerifyContactOtpResponseDto
                {
                    Success = false,
                    Message = "OTP not found. Please request a new one."
                };

            if (DateTime.UtcNow > otp.OtpExpiresAt)
                return new VerifyContactOtpResponseDto
                {
                    Success = false,
                    Message = "OTP expired. Please request a new one."
                };

            if (otp.OtpAttempts >= 3)
                return new VerifyContactOtpResponseDto
                {
                    Success = false,
                    Message = "Too many failed attempts. Please request a new OTP."
                };

            var isValid = BCrypt.Net.BCrypt.Verify(request.OtpCode, otp.OtpCode);
            if (!isValid)
            {
                otp.OtpAttempts++;
                await _context.SaveChangesAsync();
                return new VerifyContactOtpResponseDto
                {
                    Success = false,
                    Message = $"Invalid OTP. {3 - otp.OtpAttempts} attempt(s) remaining."
                };
            }

            // ── Mark OTP verified + update session ─────────
            otp.IsVerified = true;
            session.MobileVerified = true;
            session.LastCompletedStep = Math.Max(session.LastCompletedStep, 3); 

            await _context.SaveChangesAsync();        

            _logger.LogInformation(
                "Step3B verified — Session:{Id}", session.SessionId);

            return new VerifyContactOtpResponseDto
            {
                Success = true,
                Message = "Mobile verified successfully.",
                EmployerRegistrationToken = sessionId, 
                StepStatus = BuildStepStatus(session)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Verify OTP error.");
            return new VerifyContactOtpResponseDto
            {
                Success = false,
                Message = "An error occurred. Please try again."
            };
        }
    }

    // ════════════════════════════════════════════════
    // STEP 4 — Upload Licences → update DB
    // ════════════════════════════════════════════════
    public async Task<LicencesResponseDto> UploadLicencesAsync(
        LicencesRequestDto request, string sessionId)
    {
        try
        {
            var session = await GetValidSessionAsync(sessionId);
            if (session == null)
                return new LicencesResponseDto
                {
                    Success = false,
                    Message = "Session expired. Please start again."
                };

            if (session.LastCompletedStep < 3)
                return new LicencesResponseDto
                {
                    Success = false,
                    Message = "Please complete Step 3 (Contact & OTP) first."
                };

            if (request.SkipLicences)
            {
                // ✅ Mark as skipped and save to DB
                session.LicencesSkipped = true;
                session.CurrentStep = 4;
                session.LastCompletedStep = Math.Max(session.LastCompletedStep, 4);
                await _context.SaveChangesAsync();

                return new LicencesResponseDto
                {
                    Success = true,
                    Message = "Licences skipped. You can upload later from dashboard.",
                    StepStatus = BuildStepStatus(session)
                };
            }

            var allowedTypes = new[] { "application/pdf", "image/jpeg", "image/png" };
            const long maxSize = 5 * 1024 * 1024;
            var badgesEarned = new List<string>();

            if (request.PoeLicence != null && request.PoeLicence.Length > 0)
            {
                if (!allowedTypes.Contains(request.PoeLicence.ContentType))
                    return new LicencesResponseDto
                    {
                        Success = false,
                        Message = "POE licence must be PDF, JPG or PNG."
                    };

                if (request.PoeLicence.Length > maxSize)
                    return new LicencesResponseDto
                    {
                        Success = false,
                        Message = "POE licence must be under 5MB."
                    };

                // TODO: S3 upload
                session.PoeLicenceS3Url =
                    $"https://s3.amazonaws.com/skillbridge/poe/{Guid.NewGuid()}.pdf";
                badgesEarned.Add("Recruitment_Licensed");
            }

            if (request.RpslLicence != null && request.RpslLicence.Length > 0)
            {
                if (!allowedTypes.Contains(request.RpslLicence.ContentType))
                    return new LicencesResponseDto
                    {
                        Success = false,
                        Message = "RPSL licence must be PDF, JPG or PNG."
                    };

                if (request.RpslLicence.Length > maxSize)
                    return new LicencesResponseDto
                    {
                        Success = false,
                        Message = "RPSL licence must be under 5MB."
                    };

                // TODO: S3 upload
                session.RpslLicenceS3Url =
                    $"https://s3.amazonaws.com/skillbridge/rpsl/{Guid.NewGuid()}.pdf";
                badgesEarned.Add("RPSL_Licensed");
            }

            // ✅ Save to DB immediately
            session.LicencesSkipped = false;
            session.CurrentStep = 4;
            session.LastCompletedStep = Math.Max(session.LastCompletedStep, 4);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Step4 saved — Session:{Id}", session.SessionId);

            return new LicencesResponseDto
            {
                Success = true,
                Message = badgesEarned.Count > 0
                    ? "Licences uploaded. Pending admin review."
                    : "No licences uploaded.",
                PoeLicenceUrl = session.PoeLicenceS3Url,
                RpslLicenceUrl = session.RpslLicenceS3Url,
                BadgesEarned = badgesEarned,
                StepStatus = BuildStepStatus(session)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload licences error.");
            return new LicencesResponseDto
            {
                Success = false,
                Message = "An error occurred. Please try again."
            };
        }
    }

    // ════════════════════════════════════════════════
    // STEP 5 — Submit → read from DB session
    // ════════════════════════════════════════════════
    public async Task<ReviewSubmitResponseDto> SubmitRegistrationAsync(
        ReviewSubmitRequestDto request, string ipAddress)
    {
        try
        {
            if (!request.ConsentGiven)
                return new ReviewSubmitResponseDto
                {
                    Success = false,
                    Message = "You must accept the terms and conditions."
                };

            var session = await GetValidSessionAsync(request.SessionId);
            if (session == null)
                return new ReviewSubmitResponseDto
                {
                    Success = false,
                    Message = "Session expired. Please start again."
                };

            // ── Validate all required steps done ───────────
            if (!session.MobileVerified)
                return new ReviewSubmitResponseDto
                {
                    Success = false,
                    Message = "Mobile number not verified. Please complete Step 3."
                };

            if (session.LastCompletedStep < 3)
                return new ReviewSubmitResponseDto
                {
                    Success = false,
                    Message = $"Please complete all steps. Last completed: Step {session.LastCompletedStep}."
                };

            // ── Create User ────────────────────────────────
            var user = new User
            {
                UserId = Guid.NewGuid(),
                UserType = UserType.Recruiter,
                MobileNumber = session.MobileNumber!,
                CountryCode = session.CountryCode!,
                Email = session.CompanyEmail,
                PasswordHash = "N/A",
                AccountStatus = AccountStatus.Pending,
                KycStatus = KycStatus.Pending,
                PaymentStatus = PaymentStatus.Unpaid
     
            };
            _context.Users.Add(user);

            // ── Create EmployerProfile from session data ───
            var employer = new EmployerProfile
            {
                EmployerId = Guid.NewGuid(),
                UserId = user.UserId,
                LegalName = session.LegalName!,
                TradeName = session.TradeName,
                CompanyDisplayName = session.CompanyDisplayName!,
                BusinessType = Enum.Parse<BusinessType>(session.BusinessType!, true),
                IndustryType = Enum.Parse<IndustryType>(session.IndustryType!, true),
                CompanySize = Enum.Parse<CompanySize>(session.CompanySize!, true),
                Cin = session.Cin,
                WebsiteUrl = session.WebsiteUrl,
                CompanyLogoUrl = session.CompanyLogoUrl,
                GstRegistered = session.GstRegistered ?? false,
                Gstn = session.Gstn,
                Pan = session.Pan,
                GstnRegistrationDate = session.GstnRegistrationDate,
                State = session.State!,
                City = session.City!,
                Pincode = session.Pincode!,
                AddressLine1 = session.AddressLine1!,
                AddressLine2 = session.AddressLine2,
                Country = "India",
                ContactPersonName = session.ContactPersonName!,
                Designation = session.Designation!,
                ContactEmailPublic = session.CompanyEmail,
                ContactPhone = $"{session.CountryCode}{session.MobileNumber}",
                CompanyDescription = session.CompanyDescription,
                PoeLicenceS3Url = session.PoeLicenceS3Url,
                RpslLicenceS3Url = session.RpslLicenceS3Url,
                AccountStatus = AccountStatus.Pending,
                SecurityDepositPaid = false,
                ProfileCompletionScore = 60,
                ConsentTimestamp = DateTime.UtcNow,
     
            };
            _context.EmployerProfiles.Add(employer);

            // ── Wallet ─────────────────────────────────────
            _context.CreditWallets.Add(new CreditWallet
            {
                Wallet_Id = Guid.NewGuid(),
                EmployerId = employer.EmployerId,
                CreditBalance = 0,
                SharedWallet = true,
            });

            // ── Notification settings ──────────────────────
            _context.EmployerNotificationSettings.Add(new EmployerNotificationSetting
            {
                NotifPrefId = Guid.NewGuid(),
                EmployerId = employer.EmployerId,
                PrefEmailEnabled = true,
                PrefPushEnabled = true,
                PrefApplicantNotify = true,
                PrefCreditExpiryEmail = true,
                PrefAvailabilityPush = true,
                SessionTimeoutMinutes = 30
            });

            // ── Mark session as completed ──────────────────
            session.IsCompleted = true;
            session.LastCompletedStep = 5;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Recruiter registered — EmployerId:{EId} IP:{IP}",
                employer.EmployerId, ipAddress);

            var requiresDeposit = session.GstRegistered == false;

            return new ReviewSubmitResponseDto
            {
                Success = true,
                Message = requiresDeposit
                    ? "Registration submitted. Please pay ₹2,000 security deposit to activate."
                    : "Registration submitted. Your account is under review.",
                EmployerId = employer.EmployerId,
                AccountStatus = "Pending",
                RequiresSecurityDeposit = requiresDeposit,
                SecurityDepositAmountRs = requiresDeposit ? 2000 : null,
                NextStep = requiresDeposit ? "pay_deposit" : "start_trial",
                RegistrationCompleted = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Submit error. IP:{IP}", ipAddress);
            return new ReviewSubmitResponseDto
            {
                Success = false,
                Message = ex.InnerException?.InnerException?.Message
                       ?? ex.InnerException?.Message
                       ?? ex.Message
            };
        }
    }

    // ════════════════════════════════════════════════
    // RESUME — get current progress from DB
    // ════════════════════════════════════════════════
    public async Task<ResumeSessionResponseDto> ResumeSessionAsync(string sessionId)
    {
        try
        {
            var session = await GetValidSessionAsync(sessionId);
            if (session == null)
                return new ResumeSessionResponseDto
                {
                    Success = false,
                    Message = "Session not found or expired."
                };

            return new ResumeSessionResponseDto
            {
                Success = true,
                Message = $"Resume from Step {session.LastCompletedStep + 1}.",
                StepStatus = BuildStepStatus(session),
                Step3Verified = session.MobileVerified,
                Step4LicencesSkipped = session.LicencesSkipped,
                Step1Data = new GstCheckResponseDto
                {
                    Success = true,
                    GstRegistered = session.GstRegistered ?? false,
                    IndustryType = session.IndustryType ?? "",
                },
                Step2Data = session.LastCompletedStep >= 2
                    ? new CompanyDetailsResponseDto
                    {
                        Success = true,
                        CompanyLogoUrl = session.CompanyLogoUrl
                    }
                    : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resume session error.");
            return new ResumeSessionResponseDto
            {
                Success = false,
                Message = "An error occurred."
            };
        }
    }

    // ── Private Helpers ───────────────────────────────────
    private async Task<RegistrationSession?> GetValidSessionAsync(string sessionId)
    {
        if (!Guid.TryParse(sessionId, out var parsedId))
            return null;

        var session = await _context.RegistrationSessions
            .FirstOrDefaultAsync(s =>
                s.SessionId == parsedId &&
                !s.IsCompleted &&
                s.ExpiresAt > DateTime.UtcNow);

        return session;
    }

    private static StepStatusDto BuildStepStatus(RegistrationSession session)
    {
        var stepNames = new Dictionary<int, string>
        {
            { 1, "GST Check" },
            { 2, "Company Details" },
            { 3, "Contact & OTP" },
            { 4, "Licences" },
            { 5, "Review & Submit" }
        };

        var completed = Enumerable.Range(1, session.LastCompletedStep)
            .Select(i => stepNames[i])
            .ToList();

        var nextStepNum = session.LastCompletedStep + 1;

        return new StepStatusDto
        {
            CurrentStep = session.CurrentStep,
            LastCompletedStep = session.LastCompletedStep,
            TotalSteps = 5,
            SessionId = session.SessionId.ToString(),
            CompletedSteps = completed,
            NextStep = nextStepNum <= 5 ? stepNames[nextStepNum] : "Submit",
            CanResume = true,
            ExpiresAt = session.ExpiresAt
        };
    }

    private static string GenerateOtp()
    {
        var bytes = new byte[4];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return (BitConverter.ToUInt32(bytes) % 1_000_000).ToString("D6");
    }

    private static string MaskMobile(string mobile)
    {
        if (mobile.Length <= 4) return "****";
        return new string('*', mobile.Length - 4) + mobile[^4..];
    }
}