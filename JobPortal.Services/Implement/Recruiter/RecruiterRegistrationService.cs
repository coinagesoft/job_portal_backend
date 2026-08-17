using JobPortal.Application.DTOs.Admin.CompanyDocuments;
using JobPortal.Application.DTOs.Recruiter;
using JobPortal.Domain.Common;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums;
using JobPortal.Domain.Enums.common;
using JobPortal.Domain.Enums.Common;
using JobPortal.Domain.Enums.RecruiterEnums;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JobPortal.Services.Implement.Recruiter;

public class RecruiterRegistrationService : IRecruiterRegistrationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<RecruiterRegistrationService> _logger;
    private readonly IFileStorageService _fileStorageService;
    private readonly ITwilioOtpService _twilioOtpService;
    private readonly IEmailService _emailService;
    private readonly IGeminiCompanyDocumentParserService _geminiCompanyDocumentParserService;

    public RecruiterRegistrationService(
        AppDbContext context,
         ILogger<RecruiterRegistrationService> logger,
         IFileStorageService fileStorageService,
         ITwilioOtpService twilioOtpService,
         IEmailService emailService,
         IGeminiCompanyDocumentParserService geminiCompanyDocumentParserService)
    {
        _context = context;
        _logger = logger;
        _fileStorageService = fileStorageService;
        _twilioOtpService = twilioOtpService;
        _emailService = emailService;
        _geminiCompanyDocumentParserService = geminiCompanyDocumentParserService;

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
                await _fileStorageService.DeleteAsync(
                    session.CompanyLogoPublicId);

                // Upload new logo
                var logo = await _fileStorageService.UploadImageAsync(
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
            session.Country = string.IsNullOrWhiteSpace(request.Country) ? "India" : request.Country;
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
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u =>
                        u.MobileNumber == request.MobileNumber &&
                        u.CountryCode == request.CountryCode &&
                        u.UserType == UserType.Recruiter);

                if (existingUser != null)
                {
                    if (!existingUser.IsDeleted)
                    {
                        return new ContactDetailsResponseDto
                        {
                            Success = false,
                            Message = "This mobile number is already registered."
                        };
                    }

                    if (existingUser.RecoveryExpiry.HasValue &&
                        existingUser.RecoveryExpiry > DateTime.UtcNow)
                    {
                        return new ContactDetailsResponseDto
                        {
                            Success = false,
                            Message = "This account is scheduled for deletion. Please log in to recover it."
                        };
                    }
                }
            }
            //------------------------------------------------
            // Duplicate check only when email changed
            //------------------------------------------------

            if (emailChanged)
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u =>
                        u.Email == request.CompanyEmail);

                if (existingUser != null)
                {
                    if (!existingUser.IsDeleted)
                    {
                        return new ContactDetailsResponseDto
                        {
                            Success = false,
                            Message = "This email is already registered."
                        };
                    }

                    if (existingUser.RecoveryExpiry.HasValue &&
                        existingUser.RecoveryExpiry > DateTime.UtcNow)
                    {
                        return new ContactDetailsResponseDto
                        {
                            Success = false,
                            Message = "This account is scheduled for deletion. Please log in to recover it."
                        };
                    }
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
            var existingUser = await _context.Users
      .FirstOrDefaultAsync(x => x.Email == request.CompanyEmail);

            if (existingUser != null)
            {
                if (!existingUser.IsDeleted)
                {
                    return new OtpResponseDto
                    {
                        Success = false,
                        Message = "This email is already registered."
                    };
                }

                if (existingUser.RecoveryExpiry.HasValue &&
                    existingUser.RecoveryExpiry > DateTime.UtcNow)
                {
                    return new OtpResponseDto
                    {
                        Success = false,
                        Message = "This account is scheduled for deletion. Please log in to recover it."
                    };
                }
            }

          

            var otp = GenerateOtp();

            // ===== QA BYPASS: real email OTP send disabled =====
            await _emailService.SendOtpEmailAsync(
                request.CompanyEmail,
                otp);
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
                    ResendCooldownSec = 30
                });

            await _context.SaveChangesAsync();

            return new OtpResponseDto
            {
                Success = true,
                Message = "Email OTP sent successfully.",
                OtpExpiresInSeconds = 300
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
            var valid = BCrypt.Net.BCrypt.Verify(
                request.EmailOtpCode,
                otpRecord.OtpCode);
            //var valid = request.EmailOtpCode == "123456";
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
            await _emailService.SendOtpEmailAsync(
                session.CompanyEmail!,
                otp);
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
                    ResendCooldownSec = 30
                });

            await _context.SaveChangesAsync();

            return new OtpResponseDto
            {
                Success = true,
                Message = "Email OTP resent successfully.",
                OtpExpiresInSeconds = 300
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
            var existingUser = await _context.Users
     .FirstOrDefaultAsync(x =>
         x.MobileNumber == request.MobileNumber &&
         x.CountryCode == request.CountryCode &&
         x.UserType == UserType.Recruiter);

            if (existingUser != null)
            {
                if (!existingUser.IsDeleted)
                {
                    return new OtpResponseDto
                    {
                        Success = false,
                        Message = "This mobile number is already registered."
                    };
                }

                if (existingUser.RecoveryExpiry.HasValue &&
                    existingUser.RecoveryExpiry > DateTime.UtcNow)
                {
                    return new OtpResponseDto
                    {
                        Success = false,
                        Message = "This account is scheduled for deletion. Please log in to recover it."
                    };
                }
            }

         

            var fullPhone = $"{request.CountryCode}{request.MobileNumber}";

            // ===== QA BYPASS: real Twilio OTP send disabled =====
            var sent = await _twilioOtpService.SendOtpAsync(fullPhone);
            //var sent = true;
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
                OtpExpiresInSeconds = 300
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
            var valid =
                await _twilioOtpService.VerifyOtpAsync(
                    fullPhone,
                    request.MobileOtpCode);
            //var valid = request.MobileOtpCode == "123456";
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
            var sent =
                await _twilioOtpService.SendOtpAsync(fullPhone);
            //var sent = true;
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
                OtpExpiresInSeconds = 300
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

    public async Task<RegistrationDocumentTypesResponseDto> GetRegistrationDocumentTypesAsync()
    {
        try
        {
            var documents = await _context.VerificationDocumentMasters
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.IsMandatory)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            var response = new RegistrationDocumentTypesResponseDto
            {
                Success = true,

                Message = "Mandatory document types loaded successfully.",

                MandatoryDocuments = documents
                    .Select(Map)
                    .ToList(),

                OptionalDocuments = new List<RegistrationDocumentTypeDto>()
            };

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error loading registration document types.");

            return new RegistrationDocumentTypesResponseDto
            {
                Success = false,

                Message =
                    ex.InnerException?.InnerException?.Message
                    ?? ex.InnerException?.Message
                    ?? ex.Message
            };
        }
    }

    //    public async Task<RegistrationDocumentsResponseDto> UploadLicensesAsync(
    //    RegistrationDocumentsRequestDto request)
    //{
    //        try
    //        {
    //            var session = await GetValidSessionAsync(request.SessionId);

    //            if (session == null)
    //            {
    //                return new RegistrationDocumentsResponseDto
    //                {
    //                    Success = false,
    //                    Message = "Session expired. Please start registration again."
    //                };
    //            }

    //            if (session.LastCompletedStep < 3)
    //            {
    //                return new RegistrationDocumentsResponseDto
    //                {
    //                    Success = false,
    //                    Message = "Please complete Step 3 (Contact & OTP) first."
    //                };
    //            }

    //            if (request.Documents == null || !request.Documents.Any())
    //            {
    //                return new RegistrationDocumentsResponseDto
    //                {
    //                    Success = false,
    //                    Message = "Please upload at least one document."
    //                };
    //            }

    //            // Load all active document masters
    //            var documentMasters = await _context.VerificationDocumentMasters
    //                .Where(x => x.IsActive)
    //                .OrderBy(x => x.DisplayOrder)
    //                .ToListAsync();

    //            // Validate mandatory documents
    //            var mandatoryDocumentIds = documentMasters
    //                .Where(x => x.IsMandatory)
    //                .Select(x => x.DocumentTypeId)
    //                .ToList();

    //            var uploadedMandatoryIds = request.Documents
    //                .Where(x => x.DocumentTypeId.HasValue)
    //                .Select(x => x.DocumentTypeId!.Value)
    //                .Distinct()
    //                .ToList();

    //            var missingMandatoryDocuments = mandatoryDocumentIds
    //                .Except(uploadedMandatoryIds)
    //                .ToList();

    //            if (missingMandatoryDocuments.Any())
    //            {
    //                var missingNames = documentMasters
    //                    .Where(x => missingMandatoryDocuments.Contains(x.DocumentTypeId))
    //                    .Select(x => x.DocumentName)
    //                    .ToList();

    //                return new RegistrationDocumentsResponseDto
    //                {
    //                    Success = false,
    //                    Message = $"Please upload all mandatory documents. Missing: {string.Join(", ", missingNames)}"
    //                };
    //            }

    //            var oldDocuments = await _context.RegistrationSessionDocuments
    //    .Where(x => x.SessionId == session.SessionId && !x.IsDeleted)
    //    .ToListAsync();

    //            foreach (var doc in oldDocuments)
    //            {
    //                if (!string.IsNullOrWhiteSpace(doc.PublicId))
    //                {
    //                    await _fileStorageService.DeleteAsync(doc.PublicId);
    //                }
    //            }

    //            _context.RegistrationSessionDocuments.RemoveRange(oldDocuments);

    //            var allowedTypes = new[]
    //            {
    //            "application/pdf",
    //            "image/jpeg",
    //            "image/jpg",
    //            "image/png"
    //        };

    //            const long maxSize = 5 * 1024 * 1024;

    //            var uploadedDocuments = new List<RegistrationUploadedDocumentDto>();
    //            foreach (var document in request.Documents)
    //            {
    //                if (document.File == null || document.File.Length == 0)
    //                {
    //                    return new RegistrationDocumentsResponseDto
    //                    {
    //                        Success = false,
    //                        Message = "One or more uploaded files are invalid."
    //                    };
    //                }

    //                if (!allowedTypes.Contains(document.File.ContentType))
    //                {
    //                    return new RegistrationDocumentsResponseDto
    //                    {
    //                        Success = false,
    //                        Message = $"{document.File.FileName} must be PDF, JPG or PNG."
    //                    };
    //                }

    //                if (document.File.Length > maxSize)
    //                {
    //                    return new RegistrationDocumentsResponseDto
    //                    {
    //                        Success = false,
    //                        Message = $"{document.File.FileName} must be under 5 MB."
    //                    };
    //                }

    //                // Validate Document Type
    //                VerificationDocumentMaster? master = null;

    //                if (document.DocumentTypeId.HasValue)
    //                {
    //                    master = documentMasters.FirstOrDefault(x =>
    //                        x.DocumentTypeId == document.DocumentTypeId.Value);

    //                    if (master == null)
    //                    {
    //                        return new RegistrationDocumentsResponseDto
    //                        {
    //                            Success = false,
    //                            Message = $"Invalid document type selected for {document.File.FileName}."
    //                        };
    //                    }




    //                }

    //                // Upload File
    //                var uploadResult = await _fileStorageService.UploadDocumentAsync(
    //                    document.File,
    //                    $"registration/{session.SessionId}/documents");

    //                if (string.IsNullOrWhiteSpace(uploadResult.Url))
    //                {
    //                    return new RegistrationDocumentsResponseDto
    //                    {
    //                        Success = false,
    //                        Message = $"Failed to upload {document.File.FileName}."
    //                    };
    //                }

    //                // Parse Document with Gemini
    //                GeminiCompanyDocumentParseResponse? parsed = null;

    //                try
    //                {
    //                    parsed = await _geminiCompanyDocumentParserService
    //                        .ParseDocumentAsync(document.File);
    //                    // If recruiter uploaded "Other Document",
    //                    // try to map Gemini detected document type
    //                    if (!document.DocumentTypeId.HasValue &&
    //                        !string.IsNullOrWhiteSpace(parsed?.DocumentType))
    //                    {
    //                        master = documentMasters.FirstOrDefault(x =>
    //      parsed.DocumentType!.Contains(
    //          x.DocumentName,
    //          StringComparison.OrdinalIgnoreCase)
    //      ||
    //      x.DocumentName.Contains(
    //          parsed.DocumentType,
    //          StringComparison.OrdinalIgnoreCase));

    //                        if (master != null)
    //                        {
    //                            document.DocumentTypeId = master.DocumentTypeId;

    //                            document.DocumentName = master.DocumentName;

    //                            document.Category = master.Category;
    //                        }
    //                    }
    //                }
    //                catch (Exception ex)
    //                {
    //                    _logger.LogWarning(ex,
    //                        "Gemini parsing failed for {FileName}",
    //                        document.File.FileName);
    //                }



    //                // Save Temporary Registration Document
    //                var registrationDocument = new RegistrationSessionDocument
    //                {
    //                    SessionId = session.SessionId,

    //                    DocumentTypeId = document.DocumentTypeId,

    //                    CustomDocumentName =
    //    document.DocumentTypeId == null
    //        ? document.DocumentName
    //        : null,

    //                    Category =
    //    document.DocumentTypeId == null
    //        ? document.Category
    //        : master?.Category,

    //                    FileName = document.File.FileName,

    //                    FileUrl = uploadResult.Url,

    //                    PublicId = uploadResult.PublicId,

    //                    UploadedAt = DateTime.UtcNow,

    //                    DetectedDocumentType = parsed?.DocumentType,

    //                    DocumentNumber = parsed?.DocumentNumber,

    //                    IssuingAuthority = parsed?.IssuingAuthority,

    //                    IssueDate = parsed?.IssueDate,

    //                    ExpiryDate = parsed?.ExpiryDate,

    //                    ParsedDataJson = parsed?.ParsedData?.GetRawText(),

    //                    AiConfidenceScore = parsed?.AiConfidenceScore
    //                };

    //                _context.RegistrationSessionDocuments.Add(registrationDocument);

    //                uploadedDocuments.Add(new RegistrationUploadedDocumentDto
    //                {
    //                    RegistrationDocumentId = registrationDocument.RegistrationDocumentId,

    //                    DocumentTypeId = registrationDocument.DocumentTypeId,

    //                    DocumentName =
    //                        registrationDocument.CustomDocumentName ??
    //                        master?.DocumentName ??
    //                        "Custom Document",

    //                    FileUrl = registrationDocument.FileUrl,

    //                    Status = "Uploaded"
    //                });
    //            }
    //            // Update Registration Step
    //            session.CurrentStep = 4;

    //            if (session.LastCompletedStep < 4)
    //            {
    //                session.LastCompletedStep = 4;
    //            }

    //            await _context.SaveChangesAsync();

    //            _logger.LogInformation(
    //                "Registration documents uploaded successfully. Session: {SessionId}",
    //                session.SessionId);

    //            return new RegistrationDocumentsResponseDto
    //            {
    //                Success = true,
    //                Message = "Documents uploaded successfully.",

    //                Documents = uploadedDocuments,

    //                StepStatus = BuildStepStatus(session)
    //            };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(
    //                ex,
    //                "Error uploading registration documents. Session: {SessionId}",
    //                request.SessionId);

    //            return new RegistrationDocumentsResponseDto
    //            {
    //                Success = false,
    //                Message =
    //                    ex.InnerException?.InnerException?.Message
    //                    ?? ex.InnerException?.Message
    //                    ?? ex.Message
    //            };
    //        }
    //    }



    // ════════════════════════════════════════════════
    // STEP 5 — Submit → read from DB session
    // ════════════════════════════════════════════════


    //public async Task<RegistrationDocumentsResponseDto> UploadLicensesAsync(
    //RegistrationDocumentsRequestDto request)
    //{
    //    try
    //    {
    //        // --------------------------------------------------
    //        // VALIDATE SESSION
    //        // --------------------------------------------------

    //        var session = await GetValidSessionAsync(request.SessionId);

    //        if (session == null)
    //        {
    //            return new RegistrationDocumentsResponseDto
    //            {
    //                Success = false,
    //                Message = "Session expired. Please start registration again."
    //            };
    //        }

    //        // --------------------------------------------------
    //        // VALIDATE PREVIOUS STEP
    //        // --------------------------------------------------

    //        if (session.LastCompletedStep < 3)
    //        {
    //            return new RegistrationDocumentsResponseDto
    //            {
    //                Success = false,
    //                Message = "Please complete Step 3 (Contact & OTP) first."
    //            };
    //        }

    //        // --------------------------------------------------
    //        // LOAD ACTIVE DOCUMENT MASTERS
    //        // --------------------------------------------------

    //        var documentMasters = await _context.VerificationDocumentMasters
    //            .Where(x => x.IsActive)
    //            .OrderBy(x => x.DisplayOrder)
    //            .ToListAsync();

    //        // --------------------------------------------------
    //        // DOCUMENTS ARE OPTIONAL DURING REGISTRATION
    //        // --------------------------------------------------
    //        //
    //        // IMPORTANT:
    //        // Mandatory document types are still marked
    //        // IsMandatory = true in the master table.
    //        //
    //        // But missing mandatory documents DO NOT block
    //        // registration anymore.
    //        //
    //        // Recruiter can:
    //        // 1. Upload them now
    //        // 2. Skip them and register
    //        // 3. Upload them later
    //        //
    //        // Therefore there is NO mandatory-document
    //        // validation here.
    //        // --------------------------------------------------

    //        // --------------------------------------------------
    //        // IF NO DOCUMENTS WERE PROVIDED
    //        // --------------------------------------------------
    //        //
    //        // Allow the recruiter to continue without uploading.
    //        //

    //        if (request.Documents == null || !request.Documents.Any())
    //        {
    //            session.CurrentStep = 4;

    //            if (session.LastCompletedStep < 4)
    //            {
    //                session.LastCompletedStep = 4;
    //            }

    //            await _context.SaveChangesAsync();

    //            return new RegistrationDocumentsResponseDto
    //            {
    //                Success = true,
    //                Message = "Documents skipped successfully.",
    //                Documents = new List<RegistrationUploadedDocumentDto>(),
    //                StepStatus = BuildStepStatus(session)
    //            };
    //        }

    //        // --------------------------------------------------
    //        // DELETE OLD SESSION DOCUMENTS
    //        // --------------------------------------------------

    //        var oldDocuments = await _context.RegistrationSessionDocuments
    //            .Where(x =>
    //                x.SessionId == session.SessionId &&
    //                !x.IsDeleted)
    //            .ToListAsync();

    //        foreach (var doc in oldDocuments)
    //        {
    //            if (!string.IsNullOrWhiteSpace(doc.PublicId))
    //            {
    //                await _fileStorageService.DeleteAsync(doc.PublicId);
    //            }
    //        }

    //        _context.RegistrationSessionDocuments.RemoveRange(oldDocuments);

    //        // --------------------------------------------------
    //        // FILE VALIDATION
    //        // --------------------------------------------------

    //        var allowedTypes = new[]
    //        {
    //        "application/pdf",
    //        "image/jpeg",
    //        "image/jpg",
    //        "image/png"
    //    };

    //        const long maxSize = 5 * 1024 * 1024;

    //        var uploadedDocuments =
    //            new List<RegistrationUploadedDocumentDto>();

    //        // --------------------------------------------------
    //        // PROCESS DOCUMENTS
    //        // --------------------------------------------------

    //        foreach (var document in request.Documents)
    //        {
    //            // ----------------------------------------------
    //            // FILE REQUIRED
    //            // ----------------------------------------------

    //            if (document.File == null ||
    //                document.File.Length == 0)
    //            {
    //                return new RegistrationDocumentsResponseDto
    //                {
    //                    Success = false,
    //                    Message = "One or more uploaded files are invalid."
    //                };
    //            }

    //            // ----------------------------------------------
    //            // FILE TYPE
    //            // ----------------------------------------------

    //            if (!allowedTypes.Contains(document.File.ContentType))
    //            {
    //                return new RegistrationDocumentsResponseDto
    //                {
    //                    Success = false,
    //                    Message =
    //                        $"{document.File.FileName} must be PDF, JPG or PNG."
    //                };
    //            }

    //            // ----------------------------------------------
    //            // FILE SIZE
    //            // ----------------------------------------------

    //            if (document.File.Length > maxSize)
    //            {
    //                return new RegistrationDocumentsResponseDto
    //                {
    //                    Success = false,
    //                    Message =
    //                        $"{document.File.FileName} must be under 5 MB."
    //                };
    //            }

    //            // ----------------------------------------------
    //            // DOCUMENT TYPE
    //            // ----------------------------------------------

    //            VerificationDocumentMaster? master = null;

    //            if (document.DocumentTypeId.HasValue)
    //            {
    //                master = documentMasters.FirstOrDefault(x =>
    //                    x.DocumentTypeId ==
    //                    document.DocumentTypeId.Value);

    //                if (master == null)
    //                {
    //                    return new RegistrationDocumentsResponseDto
    //                    {
    //                        Success = false,
    //                        Message =
    //                            $"Invalid document type selected for {document.File.FileName}."
    //                    };
    //                }
    //            }

    //            // ----------------------------------------------
    //            // UPLOAD FILE
    //            // ----------------------------------------------

    //            var uploadResult =
    //                await _fileStorageService.UploadDocumentAsync(
    //                    document.File,
    //                    $"registration/{session.SessionId}/documents");

    //            if (string.IsNullOrWhiteSpace(uploadResult.Url))
    //            {
    //                return new RegistrationDocumentsResponseDto
    //                {
    //                    Success = false,
    //                    Message =
    //                        $"Failed to upload {document.File.FileName}."
    //                };
    //            }

    //            // ----------------------------------------------
    //            // GEMINI PARSING
    //            // ----------------------------------------------

    //            GeminiCompanyDocumentParseResponse? parsed = null;

    //            try
    //            {
    //                parsed =
    //                    await _geminiCompanyDocumentParserService
    //                        .ParseDocumentAsync(document.File);

    //                // ------------------------------------------
    //                // MAP "OTHER" DOCUMENT USING GEMINI
    //                // ------------------------------------------

    //                if (!document.DocumentTypeId.HasValue &&
    //                    !string.IsNullOrWhiteSpace(parsed?.DocumentType))
    //                {
    //                    master = documentMasters.FirstOrDefault(x =>
    //                        parsed.DocumentType!.Contains(
    //                            x.DocumentName,
    //                            StringComparison.OrdinalIgnoreCase)
    //                        ||
    //                        x.DocumentName.Contains(
    //                            parsed.DocumentType,
    //                            StringComparison.OrdinalIgnoreCase));

    //                    if (master != null)
    //                    {
    //                        document.DocumentTypeId =
    //                            master.DocumentTypeId;

    //                        document.DocumentName =
    //                            master.DocumentName;

    //                        document.Category =
    //                            master.Category;
    //                    }
    //                }
    //            }
    //            catch (Exception ex)
    //            {
    //                _logger.LogWarning(
    //                    ex,
    //                    "Gemini parsing failed for {FileName}",
    //                    document.File.FileName);
    //            }

    //            // --------------------------------------------------
    //            // SAVE REGISTRATION SESSION DOCUMENT
    //            // --------------------------------------------------

    //            var registrationDocument =
    //                new RegistrationSessionDocument
    //                {
    //                    SessionId =
    //                        session.SessionId,

    //                    DocumentTypeId =
    //                        document.DocumentTypeId,

    //                    CustomDocumentName =
    //                        document.DocumentTypeId == null
    //                            ? document.DocumentName
    //                            : null,

    //                    Category =
    //                        document.DocumentTypeId == null
    //                            ? document.Category
    //                            : master?.Category,

    //                    FileName =
    //                        document.File.FileName,

    //                    FileUrl =
    //                        uploadResult.Url,

    //                    PublicId =
    //                        uploadResult.PublicId,

    //                    UploadedAt =
    //                        DateTime.UtcNow,

    //                    DetectedDocumentType =
    //                        parsed?.DocumentType,

    //                    DocumentNumber =
    //                        parsed?.DocumentNumber,

    //                    IssuingAuthority =
    //                        parsed?.IssuingAuthority,

    //                    IssueDate =
    //                        parsed?.IssueDate,

    //                    ExpiryDate =
    //                        parsed?.ExpiryDate,

    //                    ParsedDataJson =
    //                        parsed?.ParsedData?.GetRawText(),

    //                    AiConfidenceScore =
    //                        parsed?.AiConfidenceScore
    //                };

    //            _context.RegistrationSessionDocuments
    //                .Add(registrationDocument);

    //            uploadedDocuments.Add(
    //                new RegistrationUploadedDocumentDto
    //                {
    //                    RegistrationDocumentId =
    //                        registrationDocument.RegistrationDocumentId,

    //                    DocumentTypeId =
    //                        registrationDocument.DocumentTypeId,

    //                    DocumentName =
    //                        registrationDocument.CustomDocumentName
    //                        ?? master?.DocumentName
    //                        ?? "Custom Document",

    //                    FileUrl =
    //                        registrationDocument.FileUrl,

    //                    Status = "Uploaded"
    //                });
    //        }

    //        // --------------------------------------------------
    //        // COMPLETE STEP 4
    //        // --------------------------------------------------

    //        session.CurrentStep = 4;

    //        if (session.LastCompletedStep < 4)
    //        {
    //            session.LastCompletedStep = 4;
    //        }

    //        await _context.SaveChangesAsync();

    //        _logger.LogInformation(
    //            "Registration documents uploaded successfully. Session: {SessionId}",
    //            session.SessionId);

    //        // --------------------------------------------------
    //        // RESPONSE
    //        // --------------------------------------------------

    //        return new RegistrationDocumentsResponseDto
    //        {
    //            Success = true,

    //            Message = "Documents uploaded successfully.",

    //            Documents = uploadedDocuments,

    //            StepStatus = BuildStepStatus(session)
    //        };
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(
    //            ex,
    //            "Error uploading registration documents. Session: {SessionId}",
    //            request.SessionId);

    //        return new RegistrationDocumentsResponseDto
    //        {
    //            Success = false,

    //            Message =
    //                ex.InnerException?.InnerException?.Message
    //                ?? ex.InnerException?.Message
    //                ?? ex.Message
    //        };
    //    }
    //}

    public async Task<RegistrationDocumentsResponseDto> UploadLicensesAsync(
    RegistrationDocumentsRequestDto request)
    {
        try
        {
            // --------------------------------------------------
            // VALIDATE SESSION
            // --------------------------------------------------

            var session =
                await GetValidSessionAsync(request.SessionId);

            if (session == null)
            {
                return new RegistrationDocumentsResponseDto
                {
                    Success = false,
                    Message =
                        "Session expired. Please start registration again."
                };
            }


            // --------------------------------------------------
            // VALIDATE PREVIOUS STEP
            // --------------------------------------------------

            if (session.LastCompletedStep < 3)
            {
                return new RegistrationDocumentsResponseDto
                {
                    Success = false,
                    Message =
                        "Please complete Step 3 (Contact & OTP) first."
                };
            }


            // --------------------------------------------------
            // LOAD ACTIVE DOCUMENT MASTERS
            // --------------------------------------------------

            var documentMasters =
                await _context.VerificationDocumentMasters
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.DisplayOrder)
                    .ToListAsync();


            // --------------------------------------------------
            // DOCUMENTS ARE OPTIONAL DURING REGISTRATION
            // --------------------------------------------------
            //
            // Mandatory documents are still marked as:
            //
            // IsMandatory = true
            //
            // But they do not block registration.
            //
            // Recruiter can:
            //
            // 1. Upload now
            // 2. Skip
            // 3. Upload later
            //
            // --------------------------------------------------


            // --------------------------------------------------
            // IF NO DOCUMENTS WERE PROVIDED
            // --------------------------------------------------

            if (request.Documents == null ||
                !request.Documents.Any())
            {
                session.CurrentStep = 4;

                if (session.LastCompletedStep < 4)
                {
                    session.LastCompletedStep = 4;
                }

                await _context.SaveChangesAsync();

                return new RegistrationDocumentsResponseDto
                {
                    Success = true,

                    Message =
                        "Documents skipped successfully.",

                    Documents =
                        new List<RegistrationUploadedDocumentDto>(),

                    StepStatus =
                        BuildStepStatus(session)
                };
            }


            // --------------------------------------------------
            // DELETE OLD SESSION DOCUMENTS
            // --------------------------------------------------

            var oldDocuments =
                await _context.RegistrationSessionDocuments
                    .Where(x =>
                        x.SessionId == session.SessionId &&
                        !x.IsDeleted)
                    .ToListAsync();

            foreach (var doc in oldDocuments)
            {
                if (!string.IsNullOrWhiteSpace(doc.PublicId))
                {
                    await _fileStorageService
                        .DeleteAsync(doc.PublicId);
                }
            }

            _context.RegistrationSessionDocuments
                .RemoveRange(oldDocuments);


            // --------------------------------------------------
            // FILE VALIDATION
            // --------------------------------------------------

            var allowedTypes = new[]
            {
            "application/pdf",
            "image/jpeg",
            "image/jpg",
            "image/png"
        };

            const long maxSize =
                5 * 1024 * 1024;


            var uploadedDocuments =
                new List<RegistrationUploadedDocumentDto>();


            // --------------------------------------------------
            // PROCESS DOCUMENTS
            // --------------------------------------------------

            foreach (var document in request.Documents)
            {
                // ----------------------------------------------
                // FILE REQUIRED
                // ----------------------------------------------

                if (document.File == null ||
                    document.File.Length == 0)
                {
                    return new RegistrationDocumentsResponseDto
                    {
                        Success = false,

                        Message =
                            "One or more uploaded files are invalid."
                    };
                }


                // ----------------------------------------------
                // FILE TYPE
                // ----------------------------------------------

                if (!allowedTypes.Contains(
                    document.File.ContentType))
                {
                    return new RegistrationDocumentsResponseDto
                    {
                        Success = false,

                        Message =
                            $"{document.File.FileName} must be PDF, JPG or PNG."
                    };
                }


                // ----------------------------------------------
                // FILE SIZE
                // ----------------------------------------------

                if (document.File.Length > maxSize)
                {
                    return new RegistrationDocumentsResponseDto
                    {
                        Success = false,

                        Message =
                            $"{document.File.FileName} must be under 5 MB."
                    };
                }


                // ----------------------------------------------
                // DOCUMENT TYPE
                // ----------------------------------------------

                VerificationDocumentMaster? master = null;


                // ==================================================
                // EXISTING MASTER DOCUMENT
                // ==================================================
                //
                // Only use VerificationDocumentMaster when the
                // recruiter explicitly selected a DocumentTypeId.
                //
                // ==================================================

                if (document.DocumentTypeId.HasValue)
                {
                    master =
                        documentMasters.FirstOrDefault(x =>
                            x.DocumentTypeId ==
                            document.DocumentTypeId.Value);

                    if (master == null)
                    {
                        return new RegistrationDocumentsResponseDto
                        {
                            Success = false,

                            Message =
                                $"Invalid document type selected for {document.File.FileName}."
                        };
                    }
                }


                // ==================================================
                // IMPORTANT:
                // DO NOT MAP "OTHER" USING GEMINI
                // ==================================================
                //
                // If DocumentTypeId is NULL:
                //
                //     This is an Additional document.
                //
                // Gemini can detect the document type, but that
                // detected type must NOT be used to find/create
                // a VerificationDocumentMaster.
                //
                // Example:
                //
                // Gemini:
                //     "GST Registration Certificate"
                //
                // But recruiter selected "Other":
                //
                //     DocumentTypeId = NULL
                //
                // Result:
                //
                //     DocumentTypeId = NULL
                //     Category = "Additional"
                //
                // ==================================================


                // ----------------------------------------------
                // UPLOAD FILE
                // ----------------------------------------------

                var uploadResult =
                    await _fileStorageService.UploadDocumentAsync(
                        document.File,
                        $"registration/{session.SessionId}/documents");


                if (string.IsNullOrWhiteSpace(
                    uploadResult.Url))
                {
                    return new RegistrationDocumentsResponseDto
                    {
                        Success = false,

                        Message =
                            $"Failed to upload {document.File.FileName}."
                    };
                }


                // ----------------------------------------------
                // GEMINI PARSING
                // ----------------------------------------------

                GeminiCompanyDocumentParseResponse? parsed = null;

                try
                {
                    parsed =
                        await _geminiCompanyDocumentParserService
                            .ParseDocumentAsync(document.File);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,

                        "Gemini parsing failed for {FileName}",

                        document.File.FileName);
                }


                // ==================================================
                // SAVE REGISTRATION SESSION DOCUMENT
                // ==================================================

                var registrationDocument =
                    new RegistrationSessionDocument
                    {
                        SessionId =
                            session.SessionId,


                        // --------------------------------------------------
                        // MASTER DOCUMENT
                        // --------------------------------------------------
                        //
                        // Explicitly selected master:
                        //     master.DocumentTypeId
                        //
                        // Additional:
                        //     NULL
                        //
                        DocumentTypeId =
                            master?.DocumentTypeId,


                        // --------------------------------------------------
                        // CUSTOM DOCUMENT NAME
                        // --------------------------------------------------
                        //
                        // Additional documents can use the name selected
                        // by recruiter.
                        //
                        // If no custom name exists, use Gemini detected
                        // name only as descriptive information.
                        //
                        CustomDocumentName =
                            master == null
                                ? (
                                    !string.IsNullOrWhiteSpace(
                                        document.DocumentName)
                                        ? document.DocumentName
                                        : parsed?.DocumentType
                                  )
                                : null,


                        // --------------------------------------------------
                        // CATEGORY
                        // --------------------------------------------------
                        //
                        // Existing master:
                        //     master.Category
                        //
                        // Additional:
                        //     Additional
                        //
                        Category =
                            master == null
                                ? "Additional"
                                : master.Category,


                        // --------------------------------------------------
                        // FILE
                        // --------------------------------------------------

                        FileName =
                            document.File.FileName,

                        FileUrl =
                            uploadResult.Url,

                        PublicId =
                            uploadResult.PublicId,


                        // --------------------------------------------------
                        // UPLOAD DATE
                        // --------------------------------------------------

                        UploadedAt =
                            DateTime.UtcNow,


                        // --------------------------------------------------
                        // GEMINI INFORMATION
                        // --------------------------------------------------
                        //
                        // Gemini detection is stored as information only.
                        //
                        // It does NOT modify DocumentTypeId.
                        //
                        DetectedDocumentType =
                            parsed?.DocumentType,

                        DocumentNumber =
                            parsed?.DocumentNumber,

                        IssuingAuthority =
                            parsed?.IssuingAuthority,

                        IssueDate =
                            parsed?.IssueDate,

                        ExpiryDate =
                            parsed?.ExpiryDate,

                        ParsedDataJson =
                            parsed?.ParsedData?.GetRawText(),

                        AiConfidenceScore =
                            parsed?.AiConfidenceScore
                    };


                _context.RegistrationSessionDocuments
                    .Add(registrationDocument);


                // --------------------------------------------------
                // RESPONSE ITEM
                // --------------------------------------------------

                uploadedDocuments.Add(
                    new RegistrationUploadedDocumentDto
                    {
                        RegistrationDocumentId =
                            registrationDocument.RegistrationDocumentId,

                        DocumentTypeId =
                            registrationDocument.DocumentTypeId,

                        DocumentName =
                            registrationDocument.CustomDocumentName
                            ?? master?.DocumentName
                            ?? "Additional Document",

                        FileUrl =
                            registrationDocument.FileUrl,

                        Status =
                            "Uploaded"
                    });
            }


            // --------------------------------------------------
            // COMPLETE STEP 4
            // --------------------------------------------------

            session.CurrentStep = 4;

            if (session.LastCompletedStep < 4)
            {
                session.LastCompletedStep = 4;
            }


            await _context.SaveChangesAsync();


            _logger.LogInformation(
                "Registration documents uploaded successfully. Session: {SessionId}",
                session.SessionId);


            // --------------------------------------------------
            // RESPONSE
            // --------------------------------------------------

            return new RegistrationDocumentsResponseDto
            {
                Success = true,

                Message =
                    "Documents uploaded successfully.",

                Documents =
                    uploadedDocuments,

                StepStatus =
                    BuildStepStatus(session)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,

                "Error uploading registration documents. Session: {SessionId}",

                request.SessionId);


            return new RegistrationDocumentsResponseDto
            {
                Success = false,

                Message =
                    ex.InnerException?.InnerException?.Message
                    ?? ex.InnerException?.Message
                    ?? ex.Message
            };
        }
    }

    public async Task<ReviewSubmitResponseDto> SubmitRegistrationAsync(
      ReviewSubmitRequestDto request,
      string ipAddress)
    {

        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
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

                // --------------------------------------------------
                // STEP 4 IS OPTIONAL
                // --------------------------------------------------
                // Documents/licences are not required for registration.
                // Step 3 is the last mandatory step before Review & Submit.

                if (session.LastCompletedStep < 3)
                {
                    return new ReviewSubmitResponseDto
                    {
                        Success = false,
                        Message =
                            $"Please complete all required steps. Last completed: Step {session.LastCompletedStep}.",
                        StepStatus = BuildStepStatus(session)
                    };
                }

                // Step 4 is optional, so if recruiter skipped it,
                // consider Step 4 completed for registration flow.
                if (session.LastCompletedStep < 4)
                {
                    session.CurrentStep = 5;
                    session.LastCompletedStep = 4;

                    await _context.SaveChangesAsync();
                }

                // ── Guard against a session that advanced its step counter
                // without the underlying data actually being saved (e.g. a
                // client that navigated forward after a failed save). Every
                // field below is NOT NULL on EmployerProfile/User, so
                // catching gaps here avoids a raw DB constraint exception
                // leaking to the client and instead tells them exactly
                // what's missing and which step to go back to.
                var missingFields = new List<(string Field, int Step)>();

                if (string.IsNullOrWhiteSpace(session.LegalName)) missingFields.Add(("Company legal name", 2));
                if (string.IsNullOrWhiteSpace(session.CompanyDisplayName)) missingFields.Add(("Company display name", 2));
                if (string.IsNullOrWhiteSpace(session.BusinessType)) missingFields.Add(("Business type", 2));
                if (string.IsNullOrWhiteSpace(session.IndustryType)) missingFields.Add(("Industry type", 2));
                if (string.IsNullOrWhiteSpace(session.AddressLine1)) missingFields.Add(("Address", 2));
                if (string.IsNullOrWhiteSpace(session.City)) missingFields.Add(("City", 2));
                if (string.IsNullOrWhiteSpace(session.Pincode)) missingFields.Add(("Pincode", 2));
                if (string.IsNullOrWhiteSpace(session.ContactPersonName)) missingFields.Add(("Contact person name", 3));
                if (string.IsNullOrWhiteSpace(session.Designation)) missingFields.Add(("Designation", 3));
                if (string.IsNullOrWhiteSpace(session.MobileNumber)) missingFields.Add(("Mobile number", 3));
                if (string.IsNullOrWhiteSpace(session.CountryCode)) missingFields.Add(("Mobile country code", 3));
                if (string.IsNullOrWhiteSpace(session.CompanyEmail)) missingFields.Add(("Company email", 3));

                if (missingFields.Count > 0)
                {
                    var earliestStep = missingFields.Min(m => m.Step);
                    var fieldList = string.Join(", ", missingFields.Select(m => m.Field));

                    _logger.LogWarning(
                        "SubmitRegistrationAsync blocked: session {SessionId} reached step {LastCompletedStep} but is missing required fields: {MissingFields}",
                        session.SessionId, session.LastCompletedStep, fieldList);

                    return new ReviewSubmitResponseDto
                    {
                        Success = false,
                        Message =
                            $"Some required details are missing ({fieldList}). Please go back to step {earliestStep} and re-save them before continuing.",
                        StepStatus = BuildStepStatus(session)
                    };
                }

                // Duplicate mobile check
                var existingUser = await _context.Users
     .FirstOrDefaultAsync(x =>
         x.MobileNumber == session.MobileNumber &&
         x.CountryCode == session.CountryCode &&
         x.UserType == UserType.Recruiter);

                if (existingUser != null)
                {
                    if (!existingUser.IsDeleted)
                    {
                        return new ReviewSubmitResponseDto
                        {
                            Success = false,
                            Message = "This mobile number is already registered."
                        };
                    }

                    if (existingUser.RecoveryExpiry.HasValue &&
                        existingUser.RecoveryExpiry > DateTime.UtcNow)
                    {
                        return new ReviewSubmitResponseDto
                        {
                            Success = false,
                            Message = "This account is scheduled for deletion. Please log in to recover it."
                        };
                    }
                }
                // Duplicate email check
                if (!string.IsNullOrWhiteSpace(session.CompanyEmail))
                {
                    var existingEmailUser = await _context.Users
        .FirstOrDefaultAsync(x =>
            x.Email == session.CompanyEmail);

                    if (existingEmailUser != null)
                    {
                        if (!existingEmailUser.IsDeleted)
                        {
                            return new ReviewSubmitResponseDto
                            {
                                Success = false,
                                Message = "This email is already registered."
                            };
                        }

                        if (existingEmailUser.RecoveryExpiry.HasValue &&
                            existingEmailUser.RecoveryExpiry > DateTime.UtcNow)
                        {
                            return new ReviewSubmitResponseDto
                            {
                                Success = false,
                                Message = "This account is scheduled for deletion. Please log in to recover it."
                            };
                        }
                    }
                }

                // Duplicate GSTIN check — employer_profiles.gstin has a
                // unique index, so a second registration reusing the same
                // GSTIN (e.g. a repeat/test submission, or another branch
                // of the same company registering separately) would
                // otherwise crash the insert with a raw Postgres
                // constraint error instead of a clean, actionable message.
                if (!string.IsNullOrWhiteSpace(session.Gstn))
                {
                    var gstinExists = await _context.EmployerProfiles.AnyAsync(x =>
                        x.Gstin == session.Gstn);

                    if (gstinExists)
                    {
                        return new ReviewSubmitResponseDto
                        {
                            Success = false,
                            Message = "An account with this GSTIN is already registered.",
                            StepStatus = BuildStepStatus(session)
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

                    IndustryType = session.IndustryType,

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

                    Country = string.IsNullOrWhiteSpace(session.Country) ? "India" : session.Country,

                    ContactPersonName = session.ContactPersonName!,
                    Designation = session.Designation!,
                    ContactEmailPublic = session.CompanyEmail,
                    ContactPhone =
                        $"{session.CountryCode}{session.MobileNumber}",

                    CompanyDescription = session.CompanyDescription,

                  
                    AccountStatus = AccountStatus.Pending,
                    SecurityDepositPaid = false,
                    ConsentTimestamp = now,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.EmployerProfiles.Add(employer);

                // Copy registration documents to employer verification documents
                var sessionDocuments = await _context.RegistrationSessionDocuments
                    .Where(x => x.SessionId == session.SessionId && !x.IsDeleted)
                    .ToListAsync();


                foreach (var doc in sessionDocuments)
                {
                    _context.EmployerVerificationDocuments.Add(new EmployerVerificationDocument
                    {
                        DocumentId = Guid.NewGuid(),

                        EmployerId = employer.EmployerId,

                        DocumentTypeId = doc.DocumentTypeId,

                        DocumentNumber = doc.DocumentNumber,
                        IssuingAuthority = doc.IssuingAuthority,
                        IssueDate = doc.IssueDate,
                        ExpiryDate = doc.ExpiryDate,

                        FileName = doc.FileName,
                        FileUrl = doc.FileUrl,
                        PublicId = doc.PublicId,

                        CustomDocumentName = doc.CustomDocumentName,
                        Category = doc.Category,
                        UploadedAt = doc.UploadedAt,

                        Status = VerificationDocumentStatus.Pending,

                        VerifiedBy = null,
                        VerifiedAt = null,
                        Remarks = null,

                        IsDeleted = false,

                        DetectedDocumentType = doc.DetectedDocumentType,
                        AiConfidenceScore = doc.AiConfidenceScore,
                        ParsedDataJson = doc.ParsedDataJson
                    });
                }
                // Check whether all active standard documents (excluding "Other") were uploaded
                // Get all required document IDs
                var requiredDocumentIds = await _context.VerificationDocumentMasters
                    .Where(x => x.IsActive)
                    .Select(x => x.DocumentTypeId)
                    .ToListAsync();

                // Get uploaded standard document IDs from the session
                var uploadedDocumentIds = sessionDocuments
                    .Where(x => !x.IsDeleted && x.DocumentTypeId.HasValue)
                    .Select(x => x.DocumentTypeId!.Value)
                    .Distinct()
                    .ToList();

                bool hasAllRequiredDocuments =
                    requiredDocumentIds.All(id => uploadedDocumentIds.Contains(id));

                employer.ProfileCompletionScore =
                    ProfileCompletionHelper.CalculateProfileCompletionScore(
                        employer,
                        hasAllRequiredDocuments);

          

                // ── Create Wallet (10 Free Trial Credits) ─────────────────────────
                var trialCreditsGranted = 10;
                var trialExpiresAt = DateTime.UtcNow.AddDays(30);

                _context.CreditWallets.Add(new CreditWallet
                {
                    Wallet_Id = Guid.NewGuid(),

                    EmployerId = employer.EmployerId,

                    // Give every new employer 10 free credits
                    CreditBalance = trialCreditsGranted,

                    PackageName = "Free Trial",

                    // Trial validity (change/remove as needed)
                    PackExpiresAt = trialExpiresAt,

                    SharedWallet = true,

                    UpdatedAt = now
                });

                // Record the grant in the plan-purchase ledger too.
                // CreditWalletService.ReconcileWalletBalanceAsync recomputes
                // the wallet's balance as
                // (sum of EmployerPlanPurchase.Credits) - (sum of
                // CreditUsageTransactions.CreditsUsed) every time the wallet
                // is read, and overwrites CreditBalance with that figure.
                // Without a matching ledger row here, the free trial credits
                // above get silently reset to 0 the first time the wallet
                // page loads.
                _context.EmployerPlanPurchase.Add(new EmployerPlanPurchase
                {
                    EmployerCreditPlanId = Guid.NewGuid(),
                    EmployerId = employer.EmployerId,
                    PlanId = Guid.Empty,
                    PlanName = "Free Trial",
                    Credits = trialCreditsGranted,
                    Price = 0,
                    AssignedAt = now,
                    ExpiresAt = trialExpiresAt,
                    IsActive = true,
                    AssignedBy = user.UserId,
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
        });
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

            // Load uploaded registration documents
            var uploadedDocuments = await _context.RegistrationSessionDocuments
                .Where(x =>
                    x.SessionId == session.SessionId &&
                    !x.IsDeleted)
                .OrderBy(x => x.UploadedAt)
                .ToListAsync();

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
                        Country = session.Country,
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
                        Documents = uploadedDocuments.Select(d => new ResumeRegistrationDocumentDto
                        {
                            DocumentTypeId = d.DocumentTypeId,

                            DocumentName =
                                d.CustomDocumentName ??
                                d.DetectedDocumentType ??
                                "Custom Document",

                            Category = d.Category,

                            FileUrl = d.FileUrl,

                            Status = "Uploaded"
                        }).ToList()
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

    private static RegistrationDocumentTypeDto Map(
    VerificationDocumentMaster doc)
    {
        return new RegistrationDocumentTypeDto
        {
            DocumentTypeId = doc.DocumentTypeId,
            DocumentName = doc.DocumentName,
            Category = doc.Category,
            IsMandatory = doc.IsMandatory,
            AllowMultipleUploads = doc.AllowMultipleUploads,
            AllowCustomDocument = doc.AllowCustomDocument,
            RequiresVerification = doc.RequiresVerification,
            DisplayOrder = doc.DisplayOrder
        };
    }
}