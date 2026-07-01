using JobPortal.Application.DTOs.Recruiter;
using JobPortal.Application.DTOs.SubUser;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace JobPortal.Services.Implement.Recruiter;

public class SubUserService : ISubUserService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SubUserService> _logger;
    private readonly ISubUserEmailService _subUserEmailService;
    public SubUserService(
        AppDbContext context,
        ILogger<SubUserService> logger,
        ISubUserEmailService subUserEmailService)
    {
        _context = context;
        _logger = logger;
        _subUserEmailService = subUserEmailService;
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

            var items = subUsers.Select(s => new SubUserListItemDto
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
                CreatedAt = s.CreatedAt
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
                    UserType = Domain.Enums.UserType.Recruiter,
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

            var inviteLink =
    $"https://job-portal-web-phi.vercel.app/employeer/accept-invite?token={inviteToken}"; ;

            await _subUserEmailService.SendSubUserInviteAsync(
                request.SubUserEmail,
                request.SubUserName,
                employer.CompanyDisplayName,
                request.Role.ToString(),
                inviteLink,
                subUser.InviteExpiresAt!.Value);

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

            // Generate new token and reset expiry
            subUser.InviteToken = Guid.NewGuid();
            subUser.InviteExpiresAt = DateTime.UtcNow.AddHours(72);
            await _context.SaveChangesAsync();

            // TODO: Send new invite email
            _logger.LogInformation(
                "Invite resent — Token:{Token} [DEV]",
                subUser.InviteToken);

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
    public async Task<BaseSubUserResponseDto> AcceptInviteAsync(string token)
    {
        try
        {
            if (!Guid.TryParse(token, out var parsedToken))
                return new BaseSubUserResponseDto
                {
                    Success = false,
                    Message = "Invalid invite link."
                };

            var subUser = await _context.EmployerSubUsers
                .FirstOrDefaultAsync(s => s.InviteToken == parsedToken);

            if (subUser == null)
                return new BaseSubUserResponseDto
                {
                    Success = false,
                    Message = "Invalid or expired invite link."
                };

            if (subUser.InviteAccepted)
                return new BaseSubUserResponseDto
                {
                    Success = false,
                    Message = "Invite already accepted."
                };

            if (subUser.InviteExpiresAt < DateTime.UtcNow)
                return new BaseSubUserResponseDto
                {
                    Success = false,
                    Message = "Invite link has expired. Please ask for a new invite."
                };

            // ── Mark accepted ──────────────────────────────
            subUser.InviteAccepted = true;
            subUser.InviteToken = null;         // invalidate token

            // Activate user account
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
                Message = "Invite accepted. You can now log in.",
                Email = subUser.SubUserEmail
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Accept invite error.");
            return new BaseSubUserResponseDto
            {
                Success = false,
                Message = "An error occurred."
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

    // ── Helpers ───────────────────────────────────────────
    private static InviteSubUserResponseDto InviteFail(string message) =>
        new() { Success = false, Message = message };

    public async Task<BaseSubUserResponseDto> DeleteSubUserAsync(
    Guid subUserId,
    Guid employerId)
    {
        try
        {
            var subUser = await _context.EmployerSubUsers
                .FirstOrDefaultAsync(s =>
                    s.SubUserId == subUserId &&
                    s.EmployerId == employerId);

            if (subUser == null)
            {
                return new BaseSubUserResponseDto
                {
                    Success = false,
                    Message = "Sub-user not found."
                };
            }

            // Optional: remove user if not linked elsewhere
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserId == subUser.UserId);

            _context.EmployerSubUsers.Remove(subUser);

            if (user != null)
            {
                var otherLinks = await _context.EmployerSubUsers
                    .AnyAsync(s =>
                        s.UserId == user.UserId &&
                        s.SubUserId != subUserId);

                if (!otherLinks)
                {
                    _context.Users.Remove(user);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Sub-user deleted — SubUserId:{Id}",
                subUserId);

            return new BaseSubUserResponseDto
            {
                Success = true,
                Message = "Sub-user deleted successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Delete sub-user error.");

            return new BaseSubUserResponseDto
            {
                Success = false,
                Message = ex.InnerException?.Message ?? ex.Message
            };
        }
    }
}