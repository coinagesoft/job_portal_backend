using JobPortal.Application.DTOs.Recruiter;
using JobPortal.Application.DTOs.SubUser;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace JobPortal.Services.Implement.Recruiter;

public class SubUserService : ISubUserService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SubUserService> _logger;
    private readonly ISubUserEmailService _subUserEmailService;
    private readonly IConfiguration _configuration;

    public SubUserService(
        AppDbContext context,
        ILogger<SubUserService> logger,
        ISubUserEmailService subUserEmailService,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _subUserEmailService = subUserEmailService;
        _configuration = configuration;
    }

    // Frontend base URL for invite links — configurable via
    // "Frontend:BaseUrl" (appsettings / environment), falls back to
    // localhost so local dev keeps working without extra setup.
    private string BuildInviteLink(Guid token)
    {
        var baseUrl =
            _configuration["Frontend:BaseUrl"]?.TrimEnd('/')
            ?? "http://localhost:3000";

        return $"{baseUrl}/employeer/accept-invite?token={token}";
    }

    // ════════════════════════════════════════════════
    // GET ALL SUB-USERS
    // ════════════════════════════════════════════════
    public async Task<SubUserListResponseDto> GetSubUsersAsync(Guid employerId)
    {
        try
        {
            var subUsers = await _context.EmployerSubUsers
                .Where(s => s.EmployerId == employerId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var allocations = await _context.SubUserCreditAllocation
                .Where(a => a.EmployerId == employerId)
                .ToDictionaryAsync(a => a.SubUserId);

            var items = subUsers.Select(s =>
            {
                // NOTE: despite the field name, SubUserCreditAllocation.SubUserId
                // actually stores the sub-user's login identity (User.UserId) —
                // that's what deduction at unlock/download time looks up via the
                // JWT. Match on s.UserId here, not s.SubUserId (the row's own id).
                allocations.TryGetValue(s.UserId, out var allocation);

                return new SubUserListItemDto
                {
                    SubUserId = s.SubUserId,
                    EmployerId = s.EmployerId,
                    SubUserName = s.SubUserName,
                    SubUserEmail = s.SubUserEmail,
                    SubUserMobile = s.SubUserMobile ?? "",
                    CountryCode = s.SubUserCountryCode ?? "+91",
                    Role = s.SubUserRole,
                    Status = !s.InviteAccepted ? "Pending" : s.SubUserStatus,
                    InviteAccepted = s.InviteAccepted,
                    Permissions = new PermissionsDto
                    {
                        CanSearchCandidates = s.CanSearchCandidates,
                        CanUnlockProfiles = s.CanUnlockProfiles,
                        CanPostJobs = s.CanPostJobs,
                        CanManageApplications = s.CanManageApplications
                    },
                    CreatedAt = s.CreatedAt,
                    AllocatedCredits = allocation?.AllocatedCredits ?? 0,
                    UsedCredits = allocation?.UsedCredits ?? 0,
                    RemainingCredits = allocation?.RemainingCredits ?? 0
                };
            }).ToList();

            return new SubUserListResponseDto
            {
                Success = true,
                Message = "Sub-users retrieved.",
                SubUsers = items,
                TotalCount = items.Count
            };
        }
        catch (Exception ex)
        {
            return new SubUserListResponseDto
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    // ════════════════════════════════════════════════
    // INVITE SUB-USER
    // ════════════════════════════════════════════════
    public async Task<InviteSubUserResponseDto> InviteSubUserAsync(
        InviteSubUserRequestDto request, Guid employerId)
    {
        try
        {
            // ── Check employer exists ──────────────────────
            var employer = await _context.EmployerProfiles
                .FirstOrDefaultAsync(e => e.EmployerId == employerId);

            if (employer == null)
                return InviteFail("Employer not found.");

            // ── Check sub-user limit ───────────────────────
            var existingCount = await _context.EmployerSubUsers
                .CountAsync(s =>
                    s.EmployerId == employerId &&
                    s.SubUserStatus != "Deactivated");

            if (existingCount >= 10)
                return InviteFail("Maximum 10 sub-users allowed.");

            // ── Check already invited ──────────────────────
            var alreadySubUser = await _context.EmployerSubUsers
                .AnyAsync(s =>
                    s.EmployerId == employerId &&
                    (
                        s.SubUserEmail == request.SubUserEmail ||
                        s.SubUserMobile == request.SubUserMobile
                    ) &&
                    s.SubUserStatus != "Deactivated");

            if (alreadySubUser)
                return InviteFail(
                    "This email/mobile is already a sub-user for your account.");

            // ── Get permissions ────────────────────────────
            var permissions = GetRolePermissions(request.Role);

            // ── Find existing user by email/mobile ─────────
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email == request.SubUserEmail ||
                    u.MobileNumber == request.SubUserMobile);

            // ── Create user only if not exists ─────────────
            if (user == null)
            {
                user = new User
                {
                    UserId = Guid.NewGuid(),
                    UserType = Domain.Enums.common.UserType.Recruiter,
                    MobileNumber = request.SubUserMobile,
                    CountryCode = request.CountryCode,
                    Email = request.SubUserEmail,
                    PasswordHash = "INVITE_PENDING",
                    AccountStatus = Domain.Enums.common.AccountStatus.Pending,
                    KycStatus = Domain.Enums.common.KycStatus.Pending,
                    PaymentStatus = Domain.Enums.common.PaymentStatus.Unpaid,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
            }

            // ── Create Sub User ────────────────────────────
            var inviteToken = Guid.NewGuid();

            var subUser = new EmployerSubUser
            {
                SubUserId = Guid.NewGuid(),
                EmployerId = employerId,
                UserId = user.UserId,
                SubUserName = request.SubUserName,
                SubUserEmail = request.SubUserEmail,
                SubUserMobile = request.SubUserMobile,
                SubUserCountryCode = request.CountryCode,
                SubUserRole = request.Role.ToString(),
                InviteToken = inviteToken,
                InviteExpiresAt = DateTime.UtcNow.AddHours(72),
                InviteAccepted = false,
                CanSearchCandidates = permissions.CanSearchCandidates,
                CanUnlockProfiles = permissions.CanUnlockProfiles,
                CanPostJobs = permissions.CanPostJobs,
                CanManageApplications = permissions.CanManageApplications,
                SubUserStatus = "Active",
                CreatedAt = DateTime.UtcNow
            };

            _context.EmployerSubUsers.Add(subUser);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Sending invite email to {Email}", request.SubUserEmail);

            var inviteLink = BuildInviteLink(inviteToken);

            await _subUserEmailService.SendSubUserInviteAsync(
                request.SubUserEmail,
                request.SubUserName,
                employer.CompanyDisplayName,
                request.Role.ToString(),
                inviteLink,
                subUser.InviteExpiresAt!.Value);

            _logger.LogInformation("Invite email sent successfully to {Email}", request.SubUserEmail);

            _logger.LogInformation(
                "Sub-user invited — Token:{Token} Email:{Email}",
                inviteToken,
                request.SubUserEmail);


            return new InviteSubUserResponseDto
            {
                Success = true,
                Message = $"Invite sent to {request.SubUserEmail}. Expires in 72 hours.",
                SubUserId = subUser.SubUserId,
                SubUserName = subUser.SubUserName,
                Role = subUser.SubUserRole,
                Permissions = permissions,
                InviteExpiresAt = subUser.InviteExpiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invite sub-user error.");

            return new InviteSubUserResponseDto
            {
                Success = false,
                Message = ex.InnerException?.Message ?? ex.Message
            };
        }
    }

    // ════════════════════════════════════════════════
    // UPDATE SUB-USER ROLE/PERMISSIONS
    // ════════════════════════════════════════════════

    public async Task<InviteSubUserResponseDto> UpdateSubUserAsync(
        Guid subUserId, UpdateSubUserRequestDto request, Guid employerId)
    {
        try
        {
            var subUser = await _context.EmployerSubUsers
                .FirstOrDefaultAsync(s =>
                    s.SubUserId == subUserId &&
                    s.EmployerId == employerId);

            if (subUser == null)
                return InviteFail("Sub-user not found.");

            if (subUser.SubUserStatus == "Deactivated")
                return InviteFail("Cannot edit a deactivated sub-user.");

            // ── Update role ────────────────────────────────
            var defaultPermissions = GetRolePermissions(request.Role);
            subUser.SubUserRole = request.Role.ToString();

            // ── Apply permissions — role defaults OR overrides
            subUser.CanSearchCandidates = request.CanSearchCandidates
                ?? defaultPermissions.CanSearchCandidates;
            subUser.CanUnlockProfiles = request.CanUnlockProfiles
                ?? defaultPermissions.CanUnlockProfiles;
            subUser.CanPostJobs = request.CanPostJobs
                ?? defaultPermissions.CanPostJobs;
            subUser.CanManageApplications = request.CanManageApplications
                ?? defaultPermissions.CanManageApplications;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Sub-user updated — SubUserId:{Id}", subUserId);

            return new InviteSubUserResponseDto
            {
                Success = true,
                Message = "Sub-user updated successfully.",
                SubUserId = subUser.SubUserId,
                SubUserName = subUser.SubUserName,
                Role = subUser.SubUserRole,
                Permissions = new PermissionsDto
                {
                    CanSearchCandidates = subUser.CanSearchCandidates,
                    CanUnlockProfiles = subUser.CanUnlockProfiles,
                    CanPostJobs = subUser.CanPostJobs,
                    CanManageApplications = subUser.CanManageApplications
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update sub-user error.");
            return InviteFail("An error occurred.");
        }
    }

    // ════════════════════════════════════════════════
    // DEACTIVATE — revokes access immediately
    // ════════════════════════════════════════════════
    public async Task<BaseSubUserResponseDto> DeactivateSubUserAsync(
        Guid subUserId, Guid employerId)
    {
        try
        {
            var subUser = await _context.EmployerSubUsers
                .FirstOrDefaultAsync(s =>
                    s.SubUserId == subUserId &&
                    s.EmployerId == employerId);

            if (subUser == null)
                return new BaseSubUserResponseDto
                {
                    Success = false,
                    Message = "Sub-user not found."
                };

            if (subUser.SubUserStatus == "Deactivated")
                return new BaseSubUserResponseDto
                {
                    Success = false,
                    Message = "Sub-user is already deactivated."
                };

            subUser.SubUserStatus = "Deactivated";
            subUser.DeactivatedAt = DateTime.UtcNow;

            // Also deactivate their User account
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == subUser.UserId);

            if (user != null)
            {
                user.AccountStatus = Domain.Enums.common.AccountStatus.Suspended;
                user.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Sub-user deactivated — SubUserId:{Id}", subUserId);

            return new BaseSubUserResponseDto
            {
                Success = true,
                Message = "Sub-user deactivated. Access revoked immediately."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Deactivate sub-user error.");
            return new BaseSubUserResponseDto
            {
                Success = false,
                Message = "An error occurred."
            };
        }
    }

    // ════════════════════════════════════════════════
    // REACTIVATE
    // ════════════════════════════════════════════════
    public async Task<BaseSubUserResponseDto> ReactivateSubUserAsync(
        Guid subUserId, Guid employerId)
    {
        try
        {
            var subUser = await _context.EmployerSubUsers
                .FirstOrDefaultAsync(s =>
                    s.SubUserId == subUserId &&
                    s.EmployerId == employerId);

            if (subUser == null)
                return new BaseSubUserResponseDto
                {
                    Success = false,
                    Message = "Sub-user not found."
                };

            subUser.SubUserStatus = "Active";
            subUser.DeactivatedAt = null;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == subUser.UserId);

            if (user != null)
            {
                user.AccountStatus = Domain.Enums.common.AccountStatus.Active;
                user.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return new BaseSubUserResponseDto
            {
                Success = true,
                Message = "Sub-user reactivated successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reactivate sub-user error.");
            return new BaseSubUserResponseDto
            {
                Success = false,
                Message = "An error occurred."
            };
        }
    }

    // ════════════════════════════════════════════════
    // RESEND INVITE
    // ════════════════════════════════════════════════
    public async Task<BaseSubUserResponseDto> ResendInviteAsync(
        Guid subUserId, Guid employerId)
    {
        try
        {
            var subUser = await _context.EmployerSubUsers
                .FirstOrDefaultAsync(s =>
                    s.SubUserId == subUserId &&
                    s.EmployerId == employerId);

            if (subUser == null)
                return new BaseSubUserResponseDto
                {
                    Success = false,
                    Message = "Sub-user not found."
                };

            if (subUser.InviteAccepted)
                return new BaseSubUserResponseDto
                {
                    Success = false,
                    Message = "Invite already accepted."
                };

            var employer = await _context.EmployerProfiles
                .FirstOrDefaultAsync(e => e.EmployerId == employerId);

            if (employer == null)
                return new BaseSubUserResponseDto
                {
                    Success = false,
                    Message = "Employer not found."
                };

            // Generate new token and reset expiry
            subUser.InviteToken = Guid.NewGuid();
            subUser.InviteExpiresAt = DateTime.UtcNow.AddHours(72);
            await _context.SaveChangesAsync();

            var inviteLink = BuildInviteLink(subUser.InviteToken.Value);

            await _subUserEmailService.SendSubUserInviteAsync(
                subUser.SubUserEmail,
                subUser.SubUserName,
                employer.CompanyDisplayName,
                subUser.SubUserRole,
                inviteLink,
                subUser.InviteExpiresAt.Value);

            _logger.LogInformation(
                "Invite resent — Token:{Token} Email:{Email}",
                subUser.InviteToken,
                subUser.SubUserEmail);

            return new BaseSubUserResponseDto
            {
                Success = true,
                Message = "Invite resent. New link expires in 72 hours."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resend invite error.");
            return new BaseSubUserResponseDto
            {
                Success = false,
                Message = "An error occurred."
            };
        }
    }

    // ════════════════════════════════════════════════
    // ACCEPT INVITE — called by sub-user via email link
    // ════════════════════════════════════════════════
    public async Task<BaseSubUserResponseDto> AcceptInviteAsync(
     AcceptInviteRequestDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Token) ||
                !Guid.TryParse(request.Token, out var parsedToken))
            {
                return new BaseSubUserResponseDto
                {
                    Success = false,
                    Message = "Invalid invitation link."
                };
            }

            var subUser = await _context.EmployerSubUsers
                .FirstOrDefaultAsync(s => s.InviteToken == parsedToken);

            if (subUser == null)
            {
                return new BaseSubUserResponseDto
                {
                    Success = false,
                    Message = "Invalid or expired invitation link."
                };
            }

            if (subUser.InviteAccepted)
            {
                return new BaseSubUserResponseDto
                {
                    Success = false,
                    Message = "This invitation has already been accepted."
                };
            }

            if (!subUser.InviteExpiresAt.HasValue ||
                subUser.InviteExpiresAt.Value < DateTime.UtcNow)
            {
                return new BaseSubUserResponseDto
                {
                    Success = false,
                    Message = "This invitation has expired. Please request a new invitation."
                };
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == subUser.UserId);

            if (user == null)
            {
                return new BaseSubUserResponseDto
                {
                    Success = false,
                    Message = "Associated user account not found."
                };
            }

            // Activate account
            user.AccountStatus = Domain.Enums.common.AccountStatus.Active;
            user.UpdatedAt = DateTime.UtcNow;

            // Mark invitation accepted
            subUser.InviteAccepted = true;
            subUser.InviteToken = null;
            subUser.InviteExpiresAt = null;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Sub-user invitation accepted. UserId: {UserId}, Email: {Email}",
                user.UserId,
                subUser.SubUserEmail);

            return new BaseSubUserResponseDto
            {
                Success = true,
                Message = "Invitation accepted successfully. You can now sign in using OTP.",
                Email = subUser.SubUserEmail
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error accepting invitation. Token: {Token}",
                request.Token);

            return new BaseSubUserResponseDto
            {
                Success = false,
                Message = "An error occurred while accepting the invitation."
            };
        }
    }

    // ════════════════════════════════════════════════
    // GET ROLE PERMISSIONS — matches your UI Permission Matrix
    // ════════════════════════════════════════════════
    public PermissionsDto GetRolePermissions(SubUserRole role) => role switch
    {
        SubUserRole.HR_Manager => new PermissionsDto
        {
            CanSearchCandidates = true,
            CanUnlockProfiles = true,
            CanPostJobs = true,
            CanManageApplications = true
        },
        SubUserRole.Recruiter => new PermissionsDto
        {
            CanSearchCandidates = true,
            CanUnlockProfiles = true,
            CanPostJobs = false,        // ← "Post Job: No" shown in your UI
            CanManageApplications = true
        },
        SubUserRole.Viewer => new PermissionsDto
        {
            CanSearchCandidates = true,
            CanUnlockProfiles = false,
            CanPostJobs = false,
            CanManageApplications = false
        },
        _ => new PermissionsDto()
    };

    // ════════════════════════════════════════════════
    // GET MY PERMISSIONS — called by the frontend on login
    // and on every page refresh, so it always reflects the
    // caller's current, live flags rather than a stale snapshot.
    // ════════════════════════════════════════════════
    public async Task<MyPermissionsResponseDto> GetMyPermissionsAsync(
        Guid userId, Guid employerId)
    {
        var isOwner = await _context.EmployerProfiles
            .AnyAsync(e => e.EmployerId == employerId && e.UserId == userId);

        if (isOwner)
        {
            return new MyPermissionsResponseDto
            {
                Success = true,
                IsSubUser = false,
                CanSearchCandidates = true,
                CanUnlockProfiles = true,
                CanPostJobs = true,
                CanManageApplications = true
            };
        }

        var subUser = await _context.EmployerSubUsers
            .FirstOrDefaultAsync(s =>
                s.UserId == userId &&
                s.EmployerId == employerId);

        if (subUser == null)
        {
            return new MyPermissionsResponseDto
            {
                Success = false,
                IsSubUser = true,
                CanSearchCandidates = false,
                CanUnlockProfiles = false,
                CanPostJobs = false,
                CanManageApplications = false
            };
        }

        var isActive =
            subUser.InviteAccepted &&
            subUser.SubUserStatus == "Active";

        return new MyPermissionsResponseDto
        {
            Success = true,
            IsSubUser = true,
            CanSearchCandidates = isActive && subUser.CanSearchCandidates,
            CanUnlockProfiles = isActive && subUser.CanUnlockProfiles,
            CanPostJobs = isActive && subUser.CanPostJobs,
            CanManageApplications = isActive && subUser.CanManageApplications
        };
    }

    // ── Helpers ───────────────────────────────────────────
    private static InviteSubUserResponseDto InviteFail(string message) =>
        new() { Success = false, Message = message };

    public async Task<BaseSubUserResponseDto> DeleteSubUserAsync(
        Guid subUserId,
        Guid employerId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var subUser = await _context.EmployerSubUsers
                .FirstOrDefaultAsync(x =>
                    x.SubUserId == subUserId &&
                    x.EmployerId == employerId);

            if (subUser == null)
            {
                return new BaseSubUserResponseDto
                {
                    Success = false,
                    Message = "Sub-user not found."
                };
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.UserId == subUser.UserId);

            if (user == null)
            {
                return new BaseSubUserResponseDto
                {
                    Success = false,
                    Message = "User not found."
                };
            }

            // =====================================================
            // Safety check: never delete a User row that is actually
            // an employer's own owner account (cascades into
            // employer_profiles -> job_postings and hits RESTRICT)
            // =====================================================
            var isEmployerOwner = await _context.EmployerProfiles
                .AnyAsync(x => x.UserId == user.UserId);

            if (isEmployerOwner)
            {
                return new BaseSubUserResponseDto
                {
                    Success = false,
                    Message = "This account is linked to an employer's own profile and cannot be deleted as a sub-user."
                };
            }

            // =====================================================
            // Remove FK references before deleting the sub-user
            // =====================================================

            // Jobs posted by this sub-user — keep the job, drop the reference
            var jobs = await _context.JobPostings
                .Where(x => x.PostedBySubUserId == subUserId)
                .ToListAsync();

            foreach (var job in jobs)
            {
                job.PostedBySubUserId = null;
            }

            // Credit allocated specifically to this sub-user
            var creditAllocations = await _context.SubUserCreditAllocation
                .Where(x => x.SubUserId == subUserId)
                .ToListAsync();

            if (creditAllocations.Any())
                _context.SubUserCreditAllocation.RemoveRange(creditAllocations);

            // Credit usage transactions performed by this sub-user's user account
            var creditUsageTxns = await _context.CreditUsageTransactions
                .Where(x => x.ActionByUserId == user.UserId)
                .ToListAsync();

            if (creditUsageTxns.Any())
                _context.CreditUsageTransactions.RemoveRange(creditUsageTxns);

            // =====================================================
            // Delete User Sessions
            // =====================================================
            var sessions = await _context.UserSessions
                .Where(x => x.UserId == user.UserId)
                .ToListAsync();

            if (sessions.Any())
                _context.UserSessions.RemoveRange(sessions);

            // =====================================================
            // Delete OTPs
            // =====================================================
            var otps = await _context.OtpVerifications
                .Where(x => x.UserId == user.UserId)
                .ToListAsync();

            if (otps.Any())
                _context.OtpVerifications.RemoveRange(otps);

            // =====================================================
            // Delete Notifications
            // =====================================================
            var notifications = await _context.Notifications
                .Where(x => x.UserId == user.UserId)
                .ToListAsync();

            if (notifications.Any())
                _context.Notifications.RemoveRange(notifications);

            // =====================================================
            // Delete EmployerSubUser
            // =====================================================
            _context.EmployerSubUsers.Remove(subUser);

            // =====================================================
            // Delete User
            // =====================================================
            _context.Users.Remove(user);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new BaseSubUserResponseDto
            {
                Success = true,
                Message = "Sub-user deleted successfully."
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            _logger.LogError(ex,
                "DeleteSubUser failed. SubUserId:{SubUserId}",
                subUserId);

            return new BaseSubUserResponseDto
            {
                Success = false,
                Message = ex.InnerException?.Message ?? ex.Message
            };
        }
    }

    public async Task<ValidateInviteResponseDto> ValidateInviteAsync(string token)
    {
        try
        {
            _logger.LogInformation("ValidateInvite called with Token: {Token}", token);

            if (!Guid.TryParse(token, out var parsedToken))
            {
                _logger.LogWarning("Invalid GUID format. Token: {Token}", token);

                return new ValidateInviteResponseDto
                {
                    Success = false,
                    Message = "Invalid invitation link."
                };
            }

            var subUser = await _context.EmployerSubUsers
                .Include(x => x.EmployerProfile)
                .FirstOrDefaultAsync(x => x.InviteToken == parsedToken);

            if (subUser == null)
            {
                _logger.LogWarning(
                    "No sub-user found for InviteToken: {Token}",
                    parsedToken);

                return new ValidateInviteResponseDto
                {
                    Success = false,
                    Message = "Invalid invitation link."
                };
            }

            _logger.LogInformation(
                "Sub-user found. Email: {Email}, InviteAccepted: {Accepted}, ExpiresAt: {ExpiresAt}, CurrentUtc: {CurrentUtc}",
                subUser.SubUserEmail,
                subUser.InviteAccepted,
                subUser.InviteExpiresAt,
                DateTime.UtcNow);

            if (subUser.InviteAccepted)
            {
                _logger.LogWarning(
                    "Invitation already accepted. Token: {Token}",
                    parsedToken);

                return new ValidateInviteResponseDto
                {
                    Success = false,
                    Message = "Invitation already accepted."
                };
            }

            if (!subUser.InviteExpiresAt.HasValue)
            {
                _logger.LogWarning(
                    "Invite expiry is missing. Token: {Token}",
                    parsedToken);

                return new ValidateInviteResponseDto
                {
                    Success = false,
                    Message = "Invitation expiry is invalid."
                };
            }

            if (subUser.InviteExpiresAt.Value < DateTime.UtcNow)
            {
                _logger.LogWarning(
                    "Invitation expired. Expiry: {Expiry}, Current: {Current}",
                    subUser.InviteExpiresAt.Value,
                    DateTime.UtcNow);

                return new ValidateInviteResponseDto
                {
                    Success = false,
                    Message = "Invitation has expired."
                };
            }

            _logger.LogInformation(
                "Invitation validated successfully for {Email}",
                subUser.SubUserEmail);

            return new ValidateInviteResponseDto
            {
                Success = true,
                Message = "Invitation is valid.",
                CompanyName = subUser.EmployerProfile.CompanyDisplayName,
                SubUserName = subUser.SubUserName,
                Email = subUser.SubUserEmail,
                Role = subUser.SubUserRole,
                ExpiresAt = subUser.InviteExpiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "ValidateInvite failed. Token: {Token}",
                token);

            return new ValidateInviteResponseDto
            {
                Success = false,
                Message = "An error occurred while validating the invitation."
            };
        }
    }
}