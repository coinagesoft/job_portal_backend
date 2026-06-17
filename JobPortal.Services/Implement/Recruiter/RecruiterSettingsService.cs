using JobPortal.Application.DTOs.Recruiter.Settings;
using JobPortal.Domain.Entities;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;


    namespace JobPortal.Services.Implement.Recruiter
    {
        public class RecruiterSettingsService : IRecruiterSettingsService
        {
            private readonly AppDbContext _context;

            public RecruiterSettingsService(
                AppDbContext context)
            {
                _context = context;
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

            // Email validation
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var emailExists = await _context.Users
                    .AnyAsync(x =>
                        x.UserId != user.UserId &&
                        x.Email != null &&
                        x.Email.ToLower() == request.Email.ToLower());

                if (emailExists)
                {
                    return new UpdateAccountSettingsResponseDto
                    {
                        Success = false,
                        Message = "Email already exists."
                    };
                }
            }

            // Mobile validation
            if (!string.IsNullOrWhiteSpace(request.MobileNumber))
            {
                var mobileExists = await _context.Users
                    .AnyAsync(x =>
                        x.UserId != user.UserId &&
                        x.MobileNumber == request.MobileNumber);

                if (mobileExists)
                {
                    return new UpdateAccountSettingsResponseDto
                    {
                        Success = false,
                        Message = "Mobile number already exists."
                    };
                }
            }

            // PATCH logic

            if (request.ContactPersonName != null)
                employer.ContactPersonName = request.ContactPersonName;

            if (request.Designation != null)
                employer.Designation = request.Designation;

            if (request.TimeZone != null)
                employer.TimeZone = request.TimeZone;

            if (request.Email != null)
                user.Email = request.Email;

            if (request.MobileNumber != null)
                user.MobileNumber = request.MobileNumber;

            if (request.CountryCode != null)
                user.CountryCode = request.CountryCode;

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
        }
    }
