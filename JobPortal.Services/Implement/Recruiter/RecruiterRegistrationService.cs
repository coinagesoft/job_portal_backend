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
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ITwilioOtpService _twilioOtpService;
    private readonly IEmailService _emailService;
    public RecruiterRegistrationService(
        AppDbContext context,
        ILogger<RecruiterRegistrationService> logger,
         ICloudinaryService cloudinaryService,
         ITwilioOtpService twilioOtpService,
         IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _cloudinaryService = cloudinaryService;
        _twilioOtpService = twilioOtpService;
        _emailService = emailService;

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
                LastCompletedStep = 1,
                CreatedAt = DateTime.UtcNow,

                ExpiresAt = DateTime.UtcNow.AddHours(24)

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
       CompanyDetailsRequestDto request,
       string sessionId)
    {
        try
        {
            var session = await GetValidSessionAsync(sessionId);

            if (session == null)
            {
                return new CompanyDetailsResponseDto
                {
                    Success = false,
                    Message = "Session expired. Please start again."
                };
            }

            if (session.LastCompletedStep < 1)
            {
                return new CompanyDetailsResponseDto
                {
                    Success = false,
                    Message = "Please complete Step 1 (GST Check) first."
                };
            }

            // Upload logo
            if (request.CompanyLogo != null &&
                request.CompanyLogo.Length > 0)
            {
                var allowedTypes = new[]
                {
                "image/jpeg",
                "image/jpg",
                "image/png"
            };

                if (!allowedTypes.Contains(request.CompanyLogo.ContentType))
                {
                    return new CompanyDetailsResponseDto
                    {
                        Success = false,
                        Message = "Logo must be PNG or JPG."
                    };
                }

                if (request.CompanyLogo.Length > 2 * 1024 * 1024)
                {
                    return new CompanyDetailsResponseDto
                    {
                        Success = false,
                        Message = "Logo must be under 2MB."
                    };
                }

                // Remove previous logo if re-uploading
                await _cloudinaryService.DeleteAsync(
                    session.CompanyLogoPublicId);

                // Upload new logo
                var logo = await _cloudinaryService.UploadImageAsync(
                    request.CompanyLogo,
                    "jobportalrecruiter_logo/company-logos");

                if (string.IsNullOrWhiteSpace(logo.Url))
                {
                    return new CompanyDetailsResponseDto
                    {
                        Success = false,
                        Message = "Logo upload failed."
                    };
                }

                session.CompanyLogoUrl = logo.Url;
                session.CompanyLogoPublicId = logo.PublicId;
            }

            // Company Details
            session.LegalName = request.LegalName;
            session.TradeName = request.TradeName;
            session.CompanyDisplayName = request.CompanyDisplayName;

            session.BusinessType = request.BusinessType.ToString();

            session.CompanySize = request.CompanySize?.ToString();

            session.Cin = request.Cin;

            // GST Details
            session.Gstn = request.Gstn;
            session.Pan = request.Pan;
            session.GstnRegistrationDate = request.GstnRegistrationDate;

          

            // Address
            session.State = request.State;
            session.City = request.City;
            session.Pincode = request.Pincode;

            session.AddressLine1 = request.AddressLine1;
            session.AddressLine2 = request.AddressLine2;

            // Website
            session.WebsiteUrl = request.WebsiteUrl;

            // Step tracking
            session.CurrentStep = 2;

            session.LastCompletedStep =
                Math.Max(session.LastCompletedStep, 2);

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Step2 saved — Session:{Id}",
                session.SessionId);

            return new CompanyDetailsResponseDto
            {
                Success = true,
                Message = "Company details saved successfully.",

                CompanyLogoUrl = session.CompanyLogoUrl,

                StepStatus = BuildStepStatus(session)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Save company details error.");

            return new CompanyDetailsResponseDto
            {
                Success = false,
                Message =
                    ex.InnerException?.InnerException?.Message
                    ?? ex.InnerException?.Message
                    ?? ex.Message
            };
        }
    }


    // ════════════════════════════════════════════════
    // STEP 3A — Contact + Send OTP → update DB
    // ════════════════════════════════════════════════
    //public async Task<ContactDetailsResponseDto> SaveContactDetailsAsync(
    // ContactDetailsRequestDto request,
    // string sessionId)
    //{
    //    try
    //    {
    //        var session = await GetValidSessionAsync(sessionId);

    //        if (session == null)
    //        {
    //            return new ContactDetailsResponseDto
    //            {
    //                Success = false,
    //                Message = "Session expired. Please start again."
    //            };
    //        }

    //        if (session.LastCompletedStep < 2)
    //        {
    //            return new ContactDetailsResponseDto
    //            {
    //                Success = false,
    //                Message = "Please complete Step 2 (Company Details) first."
    //            };
    //        }

    //        // Mobile duplicate
    //        var mobileExists = await _context.Users
    //            .AnyAsync(u =>
    //                u.MobileNumber == request.MobileNumber &&
    //                u.CountryCode == request.CountryCode &&
    //                u.UserType == UserType.Recruiter);

    //        if (mobileExists)
    //        {
    //            return new ContactDetailsResponseDto
    //            {
    //                Success = false,
    //                Message = "This mobile number is already registered."
    //            };
    //        }

    //        // Email duplicate
    //        var emailExists = await _context.Users
    //            .AnyAsync(u => u.Email == request.CompanyEmail);

    //        if (emailExists)
    //        {
    //            return new ContactDetailsResponseDto
    //            {
    //                Success = false,
    //                Message = "This email is already registered."
    //            };
    //        }

    //        //------------------------------------------------
    //        // MOBILE OTP
    //        //------------------------------------------------

    //        var fullPhone =
    //            $"{request.CountryCode}{request.MobileNumber}";

    //        var smsSent =
    //            await _twilioOtpService.SendOtpAsync(fullPhone);

    //        if (!smsSent)
    //        {
    //            return new ContactDetailsResponseDto
    //            {
    //                Success = false,
    //                Message = "Failed to send mobile OTP."
    //            };
    //        }

    //        //------------------------------------------------
    //        // EMAIL OTP
    //        //------------------------------------------------

    //        var emailOtp = GenerateOtp();

    //        await _emailService.SendOtpEmailAsync(
    //            request.CompanyEmail,
    //            emailOtp);

    //        //------------------------------------------------
    //        // Invalidate old records
    //        //------------------------------------------------

    //        var oldOtps = await _context.OtpVerifications
    //            .Where(o =>
    //                (
    //                    o.MobileNumber == request.MobileNumber ||
    //                    o.Email == request.CompanyEmail
    //                )
    //                &&
    //                !o.IsVerified
    //            )
    //            .ToListAsync();

    //        foreach (var old in oldOtps)
    //        {
    //            old.IsVerified = true;
    //        }

    //        //------------------------------------------------
    //        // Mobile OTP metadata
    //        //------------------------------------------------

    //        _context.OtpVerifications.Add(
    //            new OtpVerification
    //            {
    //                OtpId = Guid.NewGuid(),
    //                MobileNumber = request.MobileNumber,
    //                CountryCode = request.CountryCode,
    //                Email = request.CompanyEmail,

    //                OtpCode = "TWILIO_VERIFY",

    //                OtpSentAt = DateTime.UtcNow,
    //                OtpExpiresAt = DateTime.UtcNow.AddMinutes(10),

    //                IsVerified = false,

    //                Purpose = "RecruiterRegistration",

    //                ResendCooldownSec = 60,

    //                OtpAttempts = 0
    //            });

    //        //------------------------------------------------
    //        // Email OTP
    //        //------------------------------------------------

    //        _context.OtpVerifications.Add(
    //            new OtpVerification
    //            {
    //                OtpId = Guid.NewGuid(),

    //                Email = request.CompanyEmail,

    //                MobileNumber = request.MobileNumber,

    //                CountryCode = request.CountryCode,

    //                OtpCode = BCrypt.Net.BCrypt.HashPassword(emailOtp),

    //                OtpSentAt = DateTime.UtcNow,

    //                OtpExpiresAt = DateTime.UtcNow.AddMinutes(10),

    //                IsVerified = false,

    //                Purpose = "RecruiterRegistrationEmail",

    //                ResendCooldownSec = 60,

    //                OtpAttempts = 0
    //            });

    //        //------------------------------------------------
    //        // Update Session
    //        //------------------------------------------------

    //        session.ContactPersonName = request.ContactPersonName;
    //        session.Designation = request.Designation;
    //        session.ContactPersonEmail = request.ContactPersonEmail;
    //        session.CompanyEmail = request.CompanyEmail;

    //        session.MobileNumber = request.MobileNumber;
    //        session.CountryCode = request.CountryCode;

    //        session.CompanyDescription = request.CompanyDescription;

    //        session.MobileVerified = session.MobileVerified;
    //        session.CompanyEmailVerified = session.CompanyEmailVerified;

    //        session.CurrentStep = 3;

    //        await _context.SaveChangesAsync();

    //        _logger.LogInformation(
    //            "Step3A saved — Mobile + Email OTP sent. Session:{Id}",
    //            session.SessionId);

    //        return new ContactDetailsResponseDto
    //        {
    //            Success = true,

    //            MaskedMobile = MaskMobile(request.MobileNumber),

    //            OtpExpiresInSeconds = 600,

    //            StepStatus = BuildStepStatus(session)
    //        };
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Save contact error.");

    //        return new ContactDetailsResponseDto
    //        {
    //            Success = false,
    //            Message =
    //                ex.InnerException?.InnerException?.Message
    //                ?? ex.InnerException?.Message
    //                ?? ex.Message
    //        };
    //    }
    //}

    public async Task<ContactDetailsResponseDto> SaveContactDetailsAsync(
     ContactDetailsRequestDto request,
     string sessionId)
    {
        try
        {
            var session = await GetValidSessionAsync(sessionId);

            if (session == null)
            {
                return new ContactDetailsResponseDto
                {
                    Success = false,
                    Message = "Session expired. Please start again."
                };
            }

            if (session.LastCompletedStep < 2)
            {
                return new ContactDetailsResponseDto
                {
                    Success = false,
                    Message = "Please complete Step 2 (Company Details) first."
                };
            }

            //------------------------------------------------
            // Detect changes
            //------------------------------------------------

            bool mobileChanged =
                session.MobileNumber != request.MobileNumber ||
                session.CountryCode != request.CountryCode;

            bool emailChanged =
                !string.Equals(
                    session.CompanyEmail,
                    request.CompanyEmail,
                    StringComparison.OrdinalIgnoreCase);

            //------------------------------------------------
            // Duplicate check only when mobile changed
            //------------------------------------------------

            if (mobileChanged)
            {
                var mobileExists = await _context.Users.AnyAsync(u =>
                    u.MobileNumber == request.MobileNumber &&
                    u.CountryCode == request.CountryCode &&
                    u.UserType == UserType.Recruiter);

                if (mobileExists)
                {
                    return new ContactDetailsResponseDto
                    {
                        Success = false,
                        Message = "This mobile number is already registered."
                    };
                }
            }

            //------------------------------------------------
            // Duplicate check only when email changed
            //------------------------------------------------

            if (emailChanged)
            {
                var emailExists = await _context.Users.AnyAsync(u =>
                    u.Email == request.CompanyEmail);

                if (emailExists)
                {
                    return new ContactDetailsResponseDto
                    {
                        Success = false,
                        Message = "This email is already registered."
                    };
                }
            }

            //------------------------------------------------
            // Save contact details
            //------------------------------------------------

            session.ContactPersonName = request.ContactPersonName;
            session.Designation = request.Designation;
            session.ContactPersonEmail = request.ContactPersonEmail;

            session.CompanyEmail = request.CompanyEmail;

            session.MobileNumber = request.MobileNumber;
            session.CountryCode = request.CountryCode;

            session.CompanyDescription = request.CompanyDescription;

            //------------------------------------------------
            // Reset verification ONLY if values changed
            //------------------------------------------------

            if (mobileChanged)
            {
                session.MobileVerified = false;
            }

            if (emailChanged)
            {
                session.CompanyEmailVerified = false;
            }

            //------------------------------------------------
            // Step tracking
            //------------------------------------------------

            if (session.MobileVerified && session.CompanyEmailVerified)
            {
                session.LastCompletedStep =
                    Math.Max(session.LastCompletedStep, 3);

                session.CurrentStep = 4;
            }
            else
            {
                session.CurrentStep = 3;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Step3 saved successfully. Session:{SessionId}",
                session.SessionId);

            return new ContactDetailsResponseDto
            {
                Success = true,
                Message = "Contact details saved successfully.",

                MaskedMobile = MaskMobile(request.MobileNumber),

                OtpExpiresInSeconds = 0,

                StepStatus = BuildStepStatus(session)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save contact error.");

            return new ContactDetailsResponseDto
            {
                Success = false,
                Message =
                    ex.InnerException?.InnerException?.Message
                    ?? ex.InnerException?.Message
                    ?? ex.Message
            };
        }
    }


    public async Task<OtpResponseDto> SendEmailOtpAsync(
    SendEmailOtpRequestDto request,
    string sessionId)
    {
        try
        {
            var session = await GetValidSessionAsync(sessionId);

            if (session == null)
            {
                return new OtpResponseDto
                {
                    Success = false,
                    Message = "Session expired."
                };
            }

            // Check email already exists
            var emailExists = await _context.Users
                .AnyAsync(x => x.Email == request.CompanyEmail);

            if (emailExists)
            {
                return new OtpResponseDto
                {
                    Success = false,
                    Message = "This email is already registered."
                };
            }

            var otp = GenerateOtp();

            // ===== QA BYPASS: real email OTP send disabled =====
            // await _emailService.SendOtpEmailAsync(
            //     request.CompanyEmail,
            //     otp);
            // ===== END QA BYPASS =====

            var oldOtp = await _context.OtpVerifications
                .Where(x =>
                    x.Email == request.CompanyEmail &&
                    x.Purpose == "RecruiterRegistrationEmail" &&
                    !x.IsVerified)
                .ToListAsync();

            foreach (var item in oldOtp)
                item.IsVerified = true;

            _context.OtpVerifications.Add(
                new OtpVerification
                {
                    OtpId = Guid.NewGuid(),
                    Email = request.CompanyEmail,
                    OtpCode = BCrypt.Net.BCrypt.HashPassword(otp),
                    OtpSentAt = DateTime.UtcNow,
                    OtpExpiresAt = DateTime.UtcNow.AddMinutes(10),
                    Purpose = "RecruiterRegistrationEmail",
                    IsVerified = false,
                    OtpAttempts = 0,
                    ResendCooldownSec = 60
                });

            await _context.SaveChangesAsync();

            return new OtpResponseDto
            {
                Success = true,
                Message = "Email OTP sent successfully.",
                OtpExpiresInSeconds = 600
            };
        }
        catch (Exception ex)
        {
            return new OtpResponseDto
            {
                Success = false,
                Message =
                    ex.InnerException?.InnerException?.Message
                    ?? ex.InnerException?.Message
                    ?? ex.Message
            };
        }
    }

    public async Task<OtpResponseDto> VerifyEmailOtpAsync(
    VerifyEmailOtpRequestDto request,
    string sessionId)
    {
        try
        {
            var session = await GetValidSessionAsync(sessionId);

            if (session == null)
            {
                return new OtpResponseDto
                {
                    Success = false,
                    Message = "Session expired."
                };
            }

            var otpRecord = await _context.OtpVerifications
                .Where(x =>
                    x.Email == request.CompanyEmail &&
                    x.Purpose == "RecruiterRegistrationEmail" &&
                    !x.IsVerified)
                .OrderByDescending(x => x.OtpSentAt)
                .FirstOrDefaultAsync();

            if (otpRecord == null)
            {
                return new OtpResponseDto
                {
                    Success = false,
                    Message = "OTP not found."
                };
            }

            // ===== QA BYPASS: static OTP "123456" accepted, real check disabled =====
            // var valid = BCrypt.Net.BCrypt.Verify(
            //     request.EmailOtpCode,
            //     otpRecord.OtpCode);
            var valid = request.EmailOtpCode == "123456";
            // ===== END QA BYPASS =====

            if (!valid)
            {
                otpRecord.OtpAttempts++;

                await _context.SaveChangesAsync();

                return new OtpResponseDto
                {
                    Success = false,
                    Message = "Invalid OTP."
                };
            }

            otpRecord.IsVerified = true;

            session.CompanyEmailVerified = true;
            session.CompanyEmail = request.CompanyEmail;

            // Step 3 completed only when BOTH are verified
            if (session.MobileVerified &&
     session.CompanyEmailVerified)
            {
                session.LastCompletedStep =
                    Math.Max(session.LastCompletedStep, 3);
                session.CurrentStep = 4;
            }

            await _context.SaveChangesAsync();

            return new OtpResponseDto
            {
                Success = true,
                Message = "Email verified successfully."
            };
        }
        catch (Exception ex)
        {
            return new OtpResponseDto
            {
                Success = false,
                Message =
                    ex.InnerException?.InnerException?.Message
                    ?? ex.InnerException?.Message
                    ?? ex.Message
            };
        }
    }

    public async Task<OtpResponseDto> ResendEmailOtpAsync(
    string sessionId)
    {
        try
        {
            var session = await GetValidSessionAsync(sessionId);

            if (session == null)
            {
                return new OtpResponseDto
                {
                    Success = false,
                    Message = "Session expired."
                };
            }

            var otp = GenerateOtp();

            // ===== QA BYPASS: real email OTP send disabled =====
            // await _emailService.SendOtpEmailAsync(
            //     session.CompanyEmail!,
            //     otp);
            // ===== END QA BYPASS =====

            var oldOtps = await _context.OtpVerifications
                .Where(x =>
                    x.Email == session.CompanyEmail &&
                    x.Purpose == "RecruiterRegistrationEmail" &&
                    !x.IsVerified)
                .ToListAsync();

            foreach (var item in oldOtps)
            {
                item.IsVerified = true;
            }

            _context.OtpVerifications.Add(
                new OtpVerification
                {
                    OtpId = Guid.NewGuid(),
                    Email = session.CompanyEmail!,
                    MobileNumber = session.MobileNumber!,
                    CountryCode = session.CountryCode!,
                    OtpCode = BCrypt.Net.BCrypt.HashPassword(otp),
                    OtpSentAt = DateTime.UtcNow,
                    OtpExpiresAt = DateTime.UtcNow.AddMinutes(10),
                    Purpose = "RecruiterRegistrationEmail",
                    IsVerified = false,
                    OtpAttempts = 0,
                    ResendCooldownSec = 60
                });

            await _context.SaveChangesAsync();

            return new OtpResponseDto
            {
                Success = true,
                Message = "Email OTP resent successfully.",
                OtpExpiresInSeconds = 600
            };
        }
        catch (Exception ex)
        {
            return new OtpResponseDto
            {
                Success = false,
                Message =
                     ex.InnerException?.InnerException?.Message
                     ?? ex.InnerException?.Message
                     ?? ex.Message
            };
        }
    }

    public async Task<OtpResponseDto> SendMobileOtpAsync(
        SendMobileOtpRequestDto request,
        string sessionId)
    {
        try
        {
            // Optional: Validate session
            var session = await GetValidSessionAsync(sessionId);

            if (session == null)
            {
                return new OtpResponseDto
                {
                    Success = false,
                    Message = "Session expired."
                };
            }

            // Check if mobile number already exists
            var mobileExists = await _context.Users.AnyAsync(x =>
      x.MobileNumber == request.MobileNumber);

            if (mobileExists)
            {
                return new OtpResponseDto
                {
                    Success = false,
                    Message = "This mobile number is already registered."
                };
            }

            var fullPhone = $"{request.CountryCode}{request.MobileNumber}";

            // ===== QA BYPASS: real Twilio OTP send disabled =====
            // var sent = await _twilioOtpService.SendOtpAsync(fullPhone);
            var sent = true;
            // ===== END QA BYPASS =====

            if (!sent)
            {
                return new OtpResponseDto
                {
                    Success = false,
                    Message = "Failed to send OTP."
                };
            }

            return new OtpResponseDto
            {
                Success = true,
                Message = "Mobile OTP sent successfully.",
                OtpExpiresInSeconds = 600
            };
        }
        catch (Exception ex)
        {
            return new OtpResponseDto
            {
                Success = false,
                Message =
                    ex.InnerException?.InnerException?.Message
                    ?? ex.InnerException?.Message
                    ?? ex.Message
            };
        }
    }

    public async Task<OtpResponseDto> VerifyMobileOtpAsync(
    VerifyMobileOtpRequestDto request,
    string sessionId)
    {
        try
        {
            var session = await GetValidSessionAsync(sessionId);

            if (session == null)
            {
                return new OtpResponseDto
                {
                    Success = false,
                    Message = "Session expired."
                };
            }

            var fullPhone =
                $"{request.CountryCode}{request.MobileNumber}";

            // ===== QA BYPASS: static OTP "123456" accepted, real Twilio check disabled =====
            // var valid =
            //     await _twilioOtpService.VerifyOtpAsync(
            //         fullPhone,
            //         request.MobileOtpCode);
            var valid = request.MobileOtpCode == "123456";
            // ===== END QA BYPASS =====

            if (!valid)
            {
                return new OtpResponseDto
                {
                    Success = false,
                    Message = "Invalid OTP."
                };
            }

            session.MobileVerified = true;
            session.MobileNumber = request.MobileNumber;
            session.CountryCode = request.CountryCode;

            if (session.MobileVerified &&
                session.CompanyEmailVerified)
            {
                session.LastCompletedStep =
                    Math.Max(session.LastCompletedStep, 3);
                session.CurrentStep = 4;
            }

            await _context.SaveChangesAsync();

            return new OtpResponseDto
            {
                Success = true,
                Message = "Mobile verified successfully.",
                StepStatus = BuildStepStatus(session)
            };
        }
        catch (Exception ex)
        {
            return new OtpResponseDto
            {
                Success = false,
                Message =
                    ex.InnerException?.InnerException?.Message
                    ?? ex.InnerException?.Message
                    ?? ex.Message
            };
        }
    }

    public async Task<OtpResponseDto> ResendMobileOtpAsync(
    string sessionId)
    {
        try
        {
            var session = await GetValidSessionAsync(sessionId);

            if (session == null)
            {
                return new OtpResponseDto
                {
                    Success = false,
                    Message = "Session expired."
                };
            }

            var fullPhone =
                $"{session.CountryCode}{session.MobileNumber}";

            // ===== QA BYPASS: real Twilio OTP send disabled =====
            // var sent =
            //     await _twilioOtpService.SendOtpAsync(fullPhone);
            var sent = true;
            // ===== END QA BYPASS =====

            if (!sent)
            {
                return new OtpResponseDto
                {
                    Success = false,
                    Message = "Failed to send OTP."
                };
            }

            return new OtpResponseDto
            {
                Success = true,
                Message = "Mobile OTP resent successfully.",
                OtpExpiresInSeconds = 600
            };
        }
        catch (Exception ex)
        {
            return new OtpResponseDto
            {
                Success = false,
                Message =
                    ex.InnerException?.InnerException?.Message
                    ?? ex.InnerException?.Message
                    ?? ex.Message
            };
        }
    }

    // ════════════════════════════════════════════════
    // STEP 4 — Upload Licences → update DB
    // ════════════════════════════════════════════════
    public async Task<LicencesResponseDto> UploadLicencesAsync(
       LicencesRequestDto request,
       string sessionId)
    {
        try
        {
            var session = await GetValidSessionAsync(sessionId);

            if (session == null)
            {
                return new LicencesResponseDto
                {
                    Success = false,
                    Message = "Session expired. Please start again."
                };
            }

            if (session.LastCompletedStep < 3)
            {
                return new LicencesResponseDto
                {
                    Success = false,
                    Message = "Please complete Step 3 (Contact & OTP) first."
                };
            }

            if (request.PoeLicence == null)
            {
                return new LicencesResponseDto
                {
                    Success = false,
                    Message = "POE licence is required."
                };
            }

            if (request.RpslLicence == null)
            {
                return new LicencesResponseDto
                {
                    Success = false,
                    Message = "RPSL licence is required."
                };
            }

            var allowedTypes = new[]
            {
            "application/pdf",
            "image/jpeg",
            "image/jpg",
            "image/png"
        };

            const long maxSize = 5 * 1024 * 1024;

            // POE validation
            if (!allowedTypes.Contains(request.PoeLicence.ContentType))
            {
                return new LicencesResponseDto
                {
                    Success = false,
                    Message = "POE licence must be PDF, JPG or PNG."
                };
            }

            if (request.PoeLicence.Length > maxSize)
            {
                return new LicencesResponseDto
                {
                    Success = false,
                    Message = "POE licence must be under 5MB."
                };
            }

            // RPSL validation
            if (!allowedTypes.Contains(request.RpslLicence.ContentType))
            {
                return new LicencesResponseDto
                {
                    Success = false,
                    Message = "RPSL licence must be PDF, JPG or PNG."
                };
            }

            if (request.RpslLicence.Length > maxSize)
            {
                return new LicencesResponseDto
                {
                    Success = false,
                    Message = "RPSL licence must be under 5MB."
                };
            }

            // Remove previous uploads if user reuploads
            await _cloudinaryService.DeleteAsync(session.PoeLicencePublicId);

            await _cloudinaryService.DeleteAsync(session.RpslLicencePublicId);

            // Upload POE
            var poe = await _cloudinaryService.UploadDocumentAsync(
                request.PoeLicence,
                "jobportalrecruiter_poe/licences/poe");

            if (string.IsNullOrWhiteSpace(poe.Url))
            {
                return new LicencesResponseDto
                {
                    Success = false,
                    Message = "Failed to upload POE licence."
                };
            }

            // Upload RPSL
            var rpsl = await _cloudinaryService.UploadDocumentAsync(
                request.RpslLicence,
                "jobportalrecruiter_rpsl/licences/rpsl");

            if (string.IsNullOrWhiteSpace(rpsl.Url))
            {
                // cleanup POE because RPSL failed
                await _cloudinaryService.DeleteAsync(poe.PublicId);

                return new LicencesResponseDto
                {
                    Success = false,
                    Message = "Failed to upload RPSL licence."
                };
            }

            // Save URLs + PublicIds
            session.PoeLicenceUrl = poe.Url;
            session.PoeLicencePublicId = poe.PublicId;

            session.RpslLicenceUrl = rpsl.Url;
            session.RpslLicencePublicId = rpsl.PublicId;

            session.CurrentStep = 4;

            session.LastCompletedStep =
                Math.Max(session.LastCompletedStep, 4);

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Step4 saved — Session:{Id}",
                session.SessionId);

            return new LicencesResponseDto
            {
                Success = true,
                Message = "Licences uploaded successfully. Pending admin review.",

                PoeLicenceUrl = session.PoeLicenceUrl,

                RpslLicenceUrl = session.RpslLicenceUrl,

                BadgesEarned = new List<string>
            {
                "Recruitment_Licensed",
                "RPSL_Licensed"
            },

                StepStatus = BuildStepStatus(session)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Upload licences error.");

            return new LicencesResponseDto
            {
                Success = false,
                Message =
                    ex.InnerException?.InnerException?.Message
                    ?? ex.InnerException?.Message
                    ?? ex.Message
            };
        }
    }


    // ════════════════════════════════════════════════
    // STEP 5 — Submit → read from DB session
    // ════════════════════════════════════════════════
    public async Task<ReviewSubmitResponseDto> SubmitRegistrationAsync(
      ReviewSubmitRequestDto request,
      string ipAddress)
    {
        RegistrationSession? session = null;

        using var transaction =
            await _context.Database.BeginTransactionAsync();




        try
        {

            if (!request.ConsentGiven)
            {
                return new ReviewSubmitResponseDto
                {
                    Success = false,
                    Message = "You must accept the terms and conditions."
                };
            }

            session = await GetValidSessionAsync(request.SessionId);

            if (session == null)
            {
                return new ReviewSubmitResponseDto
                {
                    Success = false,
                    Message = "Session expired. Please start again."
                };
            }

            if (!session.MobileVerified)
            {
                return new ReviewSubmitResponseDto
                {
                    Success = false,
                    Message = "Mobile number not verified.",
                    StepStatus = BuildStepStatus(session)
                };
            }

            if (!session.CompanyEmailVerified)
            {
                return new ReviewSubmitResponseDto
                {
                    Success = false,
                    Message = "Company email not verified.",
                    StepStatus = BuildStepStatus(session)
                };
            }

            // Step 4 mandatory
            if (session.LastCompletedStep < 4)
            {
                return new ReviewSubmitResponseDto
                {
                    Success = false,
                    Message =
                        $"Please complete all steps. Last completed: Step {session.LastCompletedStep}.",
                    StepStatus = BuildStepStatus(session)
                };
            }

            // Duplicate mobile check
            var mobileExists = await _context.Users.AnyAsync(x =>
     x.MobileNumber == session.MobileNumber &&
     x.CountryCode == session.CountryCode &&
     x.UserType == UserType.Recruiter);

            if (mobileExists)
            {
                return new ReviewSubmitResponseDto
                {
                    Success = false,
                    Message = "This mobile number is already registered."
                };
            }

            // Duplicate email check
            if (!string.IsNullOrWhiteSpace(session.CompanyEmail))
            {
                var emailExists = await _context.Users.AnyAsync(x =>
                    x.Email == session.CompanyEmail);

                if (emailExists)
                {
                    return new ReviewSubmitResponseDto
                    {
                        Success = false,
                        Message = "This email is already registered."
                    };
                }
            }

            var now = DateTime.UtcNow;

            // Create User
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
                PaymentStatus = PaymentStatus.Unpaid,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.Users.Add(user);

            // Create Employer Profile
            var employer = new EmployerProfile
            {
                EmployerId = Guid.NewGuid(),
                UserId = user.UserId,

                LegalName = session.LegalName!,
                TradeName = session.TradeName,
                CompanyDisplayName = session.CompanyDisplayName!,

                BusinessType = session.BusinessType,

                IndustryType =session.IndustryType,

                CompanySize =
                    !string.IsNullOrWhiteSpace(session.CompanySize)
                        ? Enum.Parse<CompanySize>(session.CompanySize, true)
                        : null,

                Cin = session.Cin,

                WebsiteUrl = session.WebsiteUrl,
                CompanyLogoUrl = session.CompanyLogoUrl,
                CompanyLogoPublicId = session.CompanyLogoPublicId,
                GstRegistered = session.GstRegistered ?? false,
                Gstin = session.Gstn,
                Pan = session.Pan,
                GstinRegistrationDate = session.GstnRegistrationDate,

                State = session.State,
                City = session.City!,
                Pincode = session.Pincode!,
                AddressLine1 = session.AddressLine1!,
                AddressLine2 = session.AddressLine2,

                Country = "India",

                ContactPersonName = session.ContactPersonName!,
                Designation = session.Designation!,
                ContactEmailPublic = session.CompanyEmail,
                ContactPhone =
                    $"{session.CountryCode}{session.MobileNumber}",

                CompanyDescription = session.CompanyDescription,

                PoeLicenceUrl = session.PoeLicenceUrl,
                PoeLicencePublicId = session.PoeLicencePublicId,
                RpslLicenceUrl = session.RpslLicenceUrl,
                RpslLicencePublicId = session.RpslLicencePublicId,
                AccountStatus = AccountStatus.Pending,

                SecurityDepositPaid = false,

                ProfileCompletionScore = 60,

                ConsentTimestamp = now,

                CreatedAt = now,
                UpdatedAt = now
            };

            _context.EmployerProfiles.Add(employer);
            // ── Create Wallet ─────────────────────────────────────
            _context.CreditWallets.Add(new CreditWallet
            {
                Wallet_Id = Guid.NewGuid(),
                EmployerId = employer.EmployerId,
                CreditBalance = 0,
                PackageName = null,
                PackExpiresAt = null,
                SharedWallet = true,
                UpdatedAt = now
            });

            // ── Create Notification Settings ─────────────────────
            _context.EmployerNotificationSettings.Add(
                new EmployerNotificationSetting
                {
                    NotifPrefId = Guid.NewGuid(),
                    EmployerId = employer.EmployerId,

                    PrefEmailEnabled = true,
                    PrefPushEnabled = true,
                    PrefApplicantNotify = true,
                    PrefCreditExpiryEmail = true,
                    PrefJobStatusUpdates = true,
                    PrefSystemMessages = true,

                    FcmToken = null,
                    SessionTimeoutMinutes = 30
                });

            // ── Mark session completed ───────────────────────────
            session.IsCompleted = true;
            session.CurrentStep = 5;
            session.LastCompletedStep = 5;

            // Save everything
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation(
                "Recruiter registered successfully. EmployerId:{EmployerId}, IP:{IP}",
                employer.EmployerId,
                ipAddress);

            var requiresDeposit = !(session.GstRegistered ?? false);

            return new ReviewSubmitResponseDto
            {
                Success = true,

                Message = requiresDeposit
                    ? "Registration submitted. Please pay ₹2,000 security deposit to activate."
                    : "Registration submitted. Your account is under review.",

                EmployerId = employer.EmployerId,

                AccountStatus = AccountStatus.Pending.ToString(),

                RequiresSecurityDeposit = requiresDeposit,

                SecurityDepositAmountRs =
                    requiresDeposit
                        ? 2000
                        : null,

                NextStep =
                    requiresDeposit
                        ? "pay_deposit"
                        : "start_trial",

                RegistrationCompleted = true,

                StepStatus = BuildStepStatus(session)
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            _logger.LogError(
                ex,
                "Submit registration failed. IP:{IP}",
                ipAddress);

            return new ReviewSubmitResponseDto
            {
                Success = false,
                Message =
                    ex.InnerException?.InnerException?.Message
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
            {
                return new ResumeSessionResponseDto
                {
                    Success = false,
                    Message = "Session not found or expired."
                };
            }

            return new ResumeSessionResponseDto
            {
                Success = true,

                Message = session.IsCompleted
                    ? "Registration already completed."
                    : $"Resume from Step {Math.Min(session.LastCompletedStep + 1, 5)}.",

                StepStatus = BuildStepStatus(session),

                // STEP 1
                Step1Data = new GstCheckResponseDto
                {
                    Success = true,
                    GstRegistered = session.GstRegistered ?? false,
                    IndustryType = session.IndustryType ?? "",
                    RegistrationSessionId = session.SessionId.ToString()
                },

                // STEP 2
                Step2Data = session.LastCompletedStep >= 2
                    ? new ResumeCompanyDetailsDto
                    {
                        LegalName = session.LegalName,
                        TradeName = session.TradeName,
                        CompanyDisplayName = session.CompanyDisplayName,

                        BusinessType = session.BusinessType,
                        CompanySize = session.CompanySize,

                        Cin = session.Cin,

                        Gstn = session.Gstn,
                        Pan = session.Pan,
                        GstnRegistrationDate = session.GstnRegistrationDate,

                        IndustryType = session.IndustryType,

                        State = session.State,
                        City = session.City,
                        Pincode = session.Pincode,

                        AddressLine1 = session.AddressLine1,
                        AddressLine2 = session.AddressLine2,

                        WebsiteUrl = session.WebsiteUrl,

                        CompanyLogoUrl = session.CompanyLogoUrl
                    }
                    : null,

                // STEP 3
                Step3Data = session.LastCompletedStep >= 3
                    ? new ResumeContactDetailsDto
                    {
                        ContactPersonName = session.ContactPersonName,
                        Designation = session.Designation,
                        ContactPersonEmail = session.ContactPersonEmail,
                        CompanyEmail = session.CompanyEmail,

                        CountryCode = session.CountryCode,
                        MobileNumber = session.MobileNumber,

                        CompanyDescription = session.CompanyDescription,

                        MobileVerified = session.MobileVerified,
                        CompanyEmailVerified = session.CompanyEmailVerified

                    }
                    : null,

                // STEP 4
                Step4Data = session.LastCompletedStep >= 4
                    ? new ResumeLicenceDetailsDto
                    {
                        PoeLicenceUrl = session.PoeLicenceUrl,
                        RpslLicenceUrl = session.RpslLicenceUrl
                    }
                    : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Resume session error. SessionId:{SessionId}",
                sessionId);

            return new ResumeSessionResponseDto
            {
                Success = false,
                Message =
                    ex.InnerException?.InnerException?.Message
                    ?? ex.InnerException?.Message
                    ?? ex.Message
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
            NextStep =
    session.IsCompleted
        ? "Completed"
        : nextStepNum <= 5
            ? stepNames[nextStepNum]
            : "Submit",
            CanResume = !session.IsCompleted,
            ExpiresAt = session.ExpiresAt,
            MobileVerified = session.MobileVerified,
            CompanyEmailVerified = session.CompanyEmailVerified
        };
    }



    private static string MaskMobile(string mobile)
    {
        if (mobile.Length <= 4) return "****";
        return new string('*', mobile.Length - 4) + mobile[^4..];
    }

    private static string GenerateOtp()
    {
        var random = new Random();

        return random.Next(100000, 999999).ToString();
    }
}