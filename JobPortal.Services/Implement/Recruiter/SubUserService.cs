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
                .Where(s => s.EmployerId == employerId && s.SubUserStatus != "Deleted")
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
        InviteSubUserRequestDto request, Guid employerId, Guid actionUserId)
    {
        try
        {
            // ── Only the employer owner may invite sub-users ───
            if (!await IsEmployerOwnerAsync(actionUserId, employerId))
                return InviteFail("Only the account owner can invite sub-users.");

            // ── Check employer exists ──────────────────────
            var employer = await _context.EmployerProfiles
                .FirstOrDefaultAsync(e => e.EmployerId == employerId);

            if (employer == null)
                return InviteFail("Employer not found.");

            // ── Check sub-user limit ───────────────────────
            var existingCount = await _context.EmployerSubUsers
                .CountAsync(s =>
                    s.EmployerId == employerId &&
                    s.SubUserStatus != "Deactivated" &&
                    s.SubUserStatus != "Deleted");

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
                    s.SubUserStatus != "Deactivated" &&
                    s.SubUserStatus != "Deleted");

            if (alreadySubUser)
                return InviteFail(
                    "This email/mobile is already a sub-user for your account.");

            // ── Permissions come straight from the checkboxes on the
            // invite form now — no more Role → defaults lookup. ────────
            var permissions = new PermissionsDto
            {
                CanSearchCandidates = request.CanSearchCandidates,
                CanUnlockProfiles = request.CanUnlockProfiles,
                CanPostJobs = request.CanPostJobs,
                CanManageApplications = request.CanManageApplications
            };

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
                SubUserRole = DeriveRoleLabel(permissions),
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
                BuildPermissionSummary(permissions),
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
        Guid subUserId, UpdateSubUserRequestDto request, Guid employerId, Guid actionUserId)
    {
        try
        {
            // ── Only the employer owner may edit sub-user permissions.
            // Without this, a sub-user calling this endpoint on their own
            // SubUserId (or another sub-user's) could change their own
            // access, since their JWT carries the same EmployerId. ──
            if (!await IsEmployerOwnerAsync(actionUserId, employerId))
                return InviteFail("Only the account owner can update sub-user permissions.");

            var subUser = await _context.EmployerSubUsers
                .FirstOrDefaultAsync(s =>
                    s.SubUserId == subUserId &&
                    s.EmployerId == employerId);

            if (subUser == null)
                return InviteFail("Sub-user not found.");

            if (subUser.SubUserStatus == "Deactivated")
                return InviteFail("Cannot edit a deactivated sub-user.");

            // ── Apply permissions directly from the checkboxes — no more
            // Role dropdown to derive defaults from. ───────────────────
            subUser.CanSearchCandidates = request.CanSearchCandidates;
            subUser.CanUnlockProfiles = request.CanUnlockProfiles;
            subUser.CanPostJobs = request.CanPostJobs;
            subUser.CanManageApplications = request.CanManageApplications;

            // Keep a human-readable role label in sync with whatever
            // permission combo was just chosen (used for display, and by
            // the HR-Manager-only view access on a few account pages).
            subUser.SubUserRole = DeriveRoleLabel(new PermissionsDto
            {
                CanSearchCandidates = subUser.CanSearchCandidates,
                CanUnlockProfiles = subUser.CanUnlockProfiles,
                CanPostJobs = subUser.CanPostJobs,
                CanManageApplications = subUser.CanManageApplications
            });

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
        Guid subUserId, Guid employerId, Guid actionUserId)
    {
        try
        {
            if (!await IsEmployerOwnerAsync(actionUserId, employerId))
                return new BaseSubUserResponseDto
                {
                    Success = false,
                    Message = "Only the account owner can deactivate sub-users."
                };

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
        Guid subUserId, Guid employerId, Guid actionUserId)
    {
        try
        {
            if (!await IsEmployerOwnerAsync(actionUserId, employerId))
                return new BaseSubUserResponseDto
                {
                    Success = false,
                    Message = "Only the account owner can reactivate sub-users."
                };

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
        Guid subUserId, Guid employerId, Guid actionUserId)
    {
        try
        {
            if (!await IsEmployerOwnerAsync(actionUserId, employerId))
                return new BaseSubUserResponseDto
                {
                    Success = false,
                    Message = "Only the account owner can resend invites."
                };

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
                BuildPermissionSummary(new PermissionsDto
                {
                    CanSearchCandidates = subUser.CanSearchCandidates,
                    CanUnlockProfiles = subUser.CanUnlockProfiles,
                    CanPostJobs = subUser.CanPostJobs,
                    CanManageApplications = subUser.CanManageApplications
                }),
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
    // DERIVE ROLE LABEL — the invite/edit form no longer has a Role
    // dropdown; permissions are now the source of truth. We still keep
    // a short label on the record for display in the sub-users list and
    // because a couple of account pages (Company Profile, Verification,
    // Sub-Users, Buy Credits, Settings) grant read-only access to
    // whoever is labeled "HR_Manager". Any combo that doesn't match one
    // of the old presets exactly is labeled "Custom".
    // ════════════════════════════════════════════════
    private static string DeriveRoleLabel(PermissionsDto p)
    {
        if (p.CanSearchCandidates && p.CanUnlockProfiles && p.CanPostJobs && p.CanManageApplications)
            return "HR_Manager";

        if (p.CanSearchCandidates && p.CanUnlockProfiles && !p.CanPostJobs && p.CanManageApplications)
            return "Recruiter";

        if (p.CanSearchCandidates && !p.CanUnlockProfiles && !p.CanPostJobs && !p.CanManageApplications)
            return "Viewer";

        return "Custom";
    }

    // ════════════════════════════════════════════════
    // BUILD PERMISSION SUMMARY — human-readable text for the invite
    // email, e.g. "Search candidates, Post jobs". Used instead of the
    // internal role label (which can be "Custom" for non-preset combos
    // and isn't meaningful to the person receiving the invite).
    // ════════════════════════════════════════════════
    private static string BuildPermissionSummary(PermissionsDto p)
    {
        var granted = new List<string>();

        if (p.CanSearchCandidates) granted.Add("Search candidates");
        if (p.CanUnlockProfiles) granted.Add("Unlock profiles");
        if (p.CanPostJobs) granted.Add("Post jobs");
        if (p.CanManageApplications) granted.Add("Manage applications");

        return granted.Count > 0
            ? string.Join(", ", granted)
            : "No permissions granted";
    }

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
                Role = "Owner",
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
                Role = null,
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
            Role = subUser.SubUserRole,
            CanSearchCandidates = isActive && subUser.CanSearchCandidates,
            CanUnlockProfiles = isActive && subUser.CanUnlockProfiles,
            CanPostJobs = isActive && subUser.CanPostJobs,
            CanManageApplications = isActive && subUser.CanManageApplications
        };
    }

    // ── Helpers ───────────────────────────────────────────
    private static InviteSubUserResponseDto InviteFail(string message) =>
        new() { Success = false, Message = message };

    // Only the actual employer account owner may invite, edit,
    // deactivate, reactivate, resend-invite, or delete sub-users.
    // Sub-users authenticate under the same EmployerId, so without
    // this check a sub-user could call these endpoints on themselves
    // (or on other sub-users) and grant themselves extra access.
    private async Task<bool> IsEmployerOwnerAsync(Guid userId, Guid employerId) =>
        await _context.EmployerProfiles
            .AnyAsync(e => e.EmployerId == employerId && e.UserId == userId);

    public async Task<BaseSubUserResponseDto> DeleteSubUserAsync(
        Guid subUserId,
        Guid employerId,
        Guid actionUserId)
    {
        // Same execution-strategy wrapper needed here as in
        // RecruiterRegistrationService.SubmitRegistrationAsync — see that
        // method's comment for why a plain BeginTransactionAsync() no
        // longer works once EnableRetryOnFailure is configured.
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (!await IsEmployerOwnerAsync(actionUserId, employerId))
                {
                    return new BaseSubUserResponseDto
                    {
                        Success = false,
                        Message = "Only the account owner can delete sub-users."
                    };
                }

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

                if (subUser.SubUserStatus == "Deleted")
                {
                    return new BaseSubUserResponseDto
                    {
                        Success = false,
                        Message = "Sub-user is already deleted."
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
                // "Delete" is a SOFT delete, not a row removal. The
                // EmployerSubUsers row (and the underlying User row) stay in
                // place forever, purely so every place that already displays
                // this person's name from a historical record — transaction
                // history, credit allocation history, CV download history,
                // job postings they made — keeps showing "Rishi" instead of
                // "Unknown user" / a bare GUID. Nothing about *access* depends
                // on the row being physically gone: login is revoked the same
                // way Deactivate revokes it (via SubUserStatus + AccountStatus
                // below), and GetSubUsersAsync filters "Deleted" out of the
                // active sub-user list so it disappears from view exactly like
                // a real delete would, without losing the name trail.
                // =====================================================

                // Credit allocated specifically to this sub-user — whatever's
                // still unspent goes back into the shared pool. Under the
                // reconciled wallet model, unspent allocations were never
                // actually subtracted from the wallet itself (only reserved),
                // so removing the allocation row is enough to make that amount
                // available for the owner to allocate elsewhere — no wallet
                // balance change needed. We log the reclaim so the owner can
                // still see where those credits went.
                //
                // NOTE: SubUserCreditAllocation.SubUserId is keyed by the
                // sub-user's actual login identity (user.UserId), the same way
                // AllocateCreditsAsync stores it — not by the EmployerSubUsers
                // row's own id (subUserId param). Querying by subUserId here
                // would silently match nothing.
                var creditAllocations = await _context.SubUserCreditAllocation
                    .Where(x => x.SubUserId == user.UserId)
                    .ToListAsync();

                var reclaimedCredits = creditAllocations.Sum(x => x.RemainingCredits);

                if (creditAllocations.Any())
                    _context.SubUserCreditAllocation.RemoveRange(creditAllocations);

                if (reclaimedCredits > 0)
                {
                    var wallet = await _context.CreditWallets
                        .FirstOrDefaultAsync(x => x.EmployerId == employerId);

                    var allocatedElsewhere = await _context.SubUserCreditAllocation
                        .Where(x => x.EmployerId == employerId && x.SubUserId != user.UserId)
                        .SumAsync(x => (int?)x.RemainingCredits) ?? 0;

                    var availableBefore = (wallet?.CreditBalance ?? 0) - allocatedElsewhere - reclaimedCredits;

                    await _context.CreditAllocationHistory.AddAsync(
                        new CreditAllocationHistory
                        {
                            HistoryId = Guid.NewGuid(),
                            EmployerId = employerId,
                            SubUserId = subUserId,
                            SubUserName = subUser.SubUserName,
                            // Negative marks this as a reclaim rather than a
                            // fresh allocation — see AllocationHistoryDto.IsReclaim.
                            CreditsAllocated = -reclaimedCredits,
                            BalanceBefore = availableBefore,
                            BalanceAfter = availableBefore + reclaimedCredits,
                            CreatedAt = DateTime.UtcNow
                        });
                }

                // Revoke access immediately — identical mechanism to Deactivate.
                subUser.SubUserStatus = "Deleted";
                subUser.DeactivatedAt = DateTime.UtcNow;
                subUser.InviteToken = null;

                user.AccountStatus = Domain.Enums.common.AccountStatus.Suspended;
                user.UpdatedAt = DateTime.UtcNow;

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
        });
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