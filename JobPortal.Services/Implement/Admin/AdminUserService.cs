using JobPortal.Application.DTOs.Admin.Users;
using JobPortal.Domain.Constants;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums;
using JobPortal.Domain.Enums.common;
using JobPortal.Infrastructure.Extensions;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IAdmin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace JobPortal.Services.Implement.Admin;

public class AdminUserService : IAdminUserService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminUserService> _logger;

    public AdminUserService(
        AppDbContext context,
        ILogger<AdminUserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SubAdminListResponseDto> GetSubAdminsAsync(
        SubAdminListRequestDto request)
    {
        try
        {
            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize is < 1 or > 100 ? 10 : request.PageSize;

            var baseQuery = _context.AdminUsers
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Role)
                .Where(x => x.AdminType == "SubAdmin" && !x.User.IsDeleted);

            // Unfiltered counts for the stat cards — computed before the
            // search/status filter is applied.
            var totalSubAdmins = await baseQuery.CountAsync();
            var activeCount = await baseQuery.CountAsync(x => x.IsActive);
            var suspendedCount = await baseQuery.CountAsync(x => !x.IsActive);

            var query = baseQuery;

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();
                query = query.Where(x =>
                    EF.Functions.ILike(x.FullName, $"%{search}%") ||
                    (x.User.Email != null && EF.Functions.ILike(x.User.Email, $"%{search}%")) ||
                    EF.Functions.ILike(x.Role.RoleName, $"%{search}%"));
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var wantsActive = request.Status.Trim()
                    .Equals("Active", StringComparison.OrdinalIgnoreCase);
                var wantsSuspended = request.Status.Trim()
                    .Equals("Suspended", StringComparison.OrdinalIgnoreCase);

                if (wantsActive)
                    query = query.Where(x => x.IsActive);
                else if (wantsSuspended)
                    query = query.Where(x => !x.IsActive);
            }

            var filteredCount = await query.CountAsync();

            // Materialize the page first — SafeDeserializePermissions can't
            // be translated to SQL, so the DTO mapping has to happen in
            // memory rather than inside the EF Core .Select().
            var pageEntities = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = pageEntities
                .Select(x => new SubAdminDto
                {
                    AdminId = x.AdminId,
                    UserId = x.UserId,
                    AdminIdentifier = x.AdminIdentifier,
                    FullName = x.FullName,
                    Email = x.User.Email ?? string.Empty,
                    MobileNumber = x.User.MobileNumber,
                    AdminType = x.AdminType,
                    RoleId = x.RoleId,
                    RoleName = x.Role.RoleName,
                    Permissions = SubAdminPermissionsDto.FromKeyList(SafeDeserializePermissions(x.Role.Permissions)),
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt,
                    LastLoginAt = x.User.LastLoginAt
                })
                .ToList();

            return new SubAdminListResponseDto
            {
                Success = true,
                Items = items,
                TotalCount = filteredCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = pageSize == 0 ? 0 : (int)Math.Ceiling(filteredCount / (double)pageSize),
                TotalSubAdmins = totalSubAdmins,
                ActiveCount = activeCount,
                SuspendedCount = suspendedCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while listing sub-admins.");

            return new SubAdminListResponseDto
            {
                Success = false,
                Message = "Unable to load sub admins. Please try again."
            };
        }
    }

    public async Task<CreateSubAdminResponseDto> CreateSubAdminAsync(
        CreateSubAdminRequestDto request,
        Guid createdByAdminId,
        string ipAddress,
        string? jwtId = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            //-------------------------------------------------------
            // 1. Authorization — only the top-level admin ("Admin" /
            //    "SuperAdmin" — this DB's existing seed data uses
            //    "Admin"), or a sub-admin whose role explicitly grants
            //    the "users" sidebar tab, may add new sub-admins.
            //-------------------------------------------------------

            var creator = await _context.AdminUsers
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.AdminId == createdByAdminId);

            if (creator == null || !creator.IsActive)
                return Fail("Admin account not found or inactive.");

            var creatorIsSuperAdmin =
                string.Equals(creator.AdminType, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(creator.AdminType, "Admin", StringComparison.OrdinalIgnoreCase);

            // "users" is the sidebar tab that shows the /admin/users page —
            // i.e. the tab that lets a sub-admin manage other sub-admins.
            // This replaces the old granular "subadmin.create" key now
            // that permissions are tab-level.
            if (!creatorIsSuperAdmin &&
                !RoleHasPermission(creator.Role?.Permissions, "users"))
            {
                return Fail("You do not have permission to create sub-admins.");
            }

            //-------------------------------------------------------
            // 2. Basic validation
            //-------------------------------------------------------

            var email = request.Email.Trim().ToLower();
            var fullName = request.FullName.Trim();
            var roleNameInput = request.RoleName.Trim();
            var permissions = request.Permissions?.ToKeyList() ?? new List<string>();

            if (permissions.Count == 0)
                return Fail("At least one permission must be turned on.");

            var mobileNumber = string.IsNullOrWhiteSpace(request.MobileNumber)
                ? null
                : request.MobileNumber.Trim();

            //-------------------------------------------------------
            // 3. Email / mobile must not already be in use
            //-------------------------------------------------------

            var emailTaken = await _context.Users
                .AnyAsync(x => x.Email == email);

            if (emailTaken)
                return Fail("A user with this email already exists.");

            if (mobileNumber != null)
            {
                var mobileTaken = await _context.Users
                    .AnyAsync(x => x.MobileNumber == mobileNumber);

                if (mobileTaken)
                    return Fail("A user with this mobile number already exists.");
            }

            //-------------------------------------------------------
            // 4. Resolve the AdminRole (reuse an existing preset role
            //    by name, or create one). "Custom" role names are
            //    made unique per sub-admin since RoleName is unique.
            //-------------------------------------------------------

            var role = await ResolveRoleAsync(roleNameInput, permissions, fullName, createdByAdminId, currentRoleId: null);

            //-------------------------------------------------------
            // 5. Create the User row backing the sub-admin. Login is
            //    OTP-based (no password), same pattern used elsewhere
            //    in this codebase (e.g. "OTP_AUTH", "GOOGLE_AUTH").
            //    UserType must stay Admin — that's what AdminAuthService
            //    .SendOtpAsync looks up at login time.
            //-------------------------------------------------------

            var user = new User
            {
                UserId = Guid.NewGuid(),
                UserType = UserType.Admin,
                Email = email,
                MobileNumber = mobileNumber,
                CountryCode = string.IsNullOrWhiteSpace(request.CountryCode)
                    ? "+91"
                    : request.CountryCode.Trim(),
                PasswordHash = "OTP_AUTH",
                AccountStatus = request.IsActive ? AccountStatus.Active : AccountStatus.Suspended,
                KycStatus = KycStatus.Approved,
                PaymentStatus = PaymentStatus.Paid,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);

            //-------------------------------------------------------
            // 6. Generate a sequential admin identifier (ADM-000001…)
            //-------------------------------------------------------

            var adminIdentifier = await GenerateAdminIdentifierAsync();

            //-------------------------------------------------------
            // 7. Create the AdminUser row
            //-------------------------------------------------------

            var admin = new AdminUser
            {
                AdminId = Guid.NewGuid(),
                UserId = user.UserId,
                AdminIdentifier = adminIdentifier,
                FullName = fullName,
                AdminType = "SubAdmin",
                RoleId = role.RoleId,
                IsActive = request.IsActive,
                CreatedBy = createdByAdminId,
                CreatedAt = DateTime.UtcNow
            };

            _context.AdminUsers.Add(admin);

            //-------------------------------------------------------
            // 8. Audit log
            //-------------------------------------------------------

            _context.AuditLogs.Add(new AuditLog
            {
                LogId = Guid.NewGuid(),
                PerformedByAdminId = creator.AdminId,
                PerformedByName = creator.FullName,
                PerformedByRole = creator.Role?.RoleName ?? creator.AdminType,
                Module = "Sub Admin",
                Action = "Create Sub Admin",
                TargetEntityType = "AdminUser",
                TargetEntityId = admin.AdminId,
                TargetEntityName = fullName,
                NewValues = JsonSerializer.Serialize(new
                {
                    admin.AdminIdentifier,
                    admin.FullName,
                    Email = email,
                    RoleName = role.RoleName,
                    Permissions = permissions,
                    admin.IsActive
                }),
                Description = $"Created sub-admin '{fullName}' ({email}) with role '{role.RoleName}'.",
                IpAddress = ipAddress,
                Success = true,
                Severity = AuditActionSeverity.Resolve("Create Sub Admin"),
                SessionId = await _context.ResolveSessionIdAsync(jwtId),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            //-------------------------------------------------------
            // 9. Response
            //-------------------------------------------------------

            return new CreateSubAdminResponseDto
            {
                Success = true,
                Message = "Sub admin created successfully.",

                SubAdmin = new SubAdminDto
                {
                    AdminId = admin.AdminId,
                    UserId = user.UserId,
                    AdminIdentifier = admin.AdminIdentifier,
                    FullName = admin.FullName,
                    Email = email,
                    MobileNumber = mobileNumber,
                    AdminType = admin.AdminType,
                    RoleId = role.RoleId,
                    RoleName = role.RoleName,
                    Permissions = SubAdminPermissionsDto.FromKeyList(permissions),
                    IsActive = admin.IsActive,
                    CreatedAt = admin.CreatedAt
                }
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            _logger.LogError(ex, "Error while creating sub-admin.");

            return Fail("Unable to create sub admin. Please try again.");
        }
    }

    public async Task<UpdateSubAdminResponseDto> UpdateSubAdminAsync(
        Guid subAdminId,
        UpdateSubAdminRequestDto request,
        Guid updatedByAdminId,
        string ipAddress,
        string? jwtId = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            //-------------------------------------------------------
            // 1. Authorization — same rule as create.
            //-------------------------------------------------------

            var updater = await _context.AdminUsers
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.AdminId == updatedByAdminId);

            if (updater == null || !updater.IsActive)
                return UpdateFail("Admin account not found or inactive.");

            var updaterIsSuperAdmin =
                string.Equals(updater.AdminType, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(updater.AdminType, "Admin", StringComparison.OrdinalIgnoreCase);

            if (!updaterIsSuperAdmin &&
                !RoleHasPermission(updater.Role?.Permissions, "users"))
            {
                return UpdateFail("You do not have permission to edit sub-admins.");
            }

            //-------------------------------------------------------
            // 2. Load target — only sub-admins can be edited here.
            //-------------------------------------------------------

            var target = await _context.AdminUsers
                .Include(x => x.Role)
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.AdminId == subAdminId);

            if (target == null || target.User == null)
                return UpdateFail("Sub admin not found.");

            if (!string.Equals(target.AdminType, "SubAdmin", StringComparison.OrdinalIgnoreCase))
                return UpdateFail("Only sub-admin accounts can be edited here.");

            if (target.User.IsDeleted)
                return UpdateFail("This sub-admin has been deleted.");

            //-------------------------------------------------------
            // 3. Basic validation
            //-------------------------------------------------------

            var fullName = request.FullName.Trim();
            var roleNameInput = request.RoleName.Trim();

            var permissions = request.Permissions?.ToKeyList() ?? new List<string>();

            if (permissions.Count == 0)
                return UpdateFail("At least one permission must be turned on.");

            var mobileNumber = string.IsNullOrWhiteSpace(request.MobileNumber)
                ? null
                : request.MobileNumber.Trim();

            if (mobileNumber != null)
            {
                var mobileTaken = await _context.Users
                    .AnyAsync(x => x.MobileNumber == mobileNumber && x.UserId != target.UserId);

                if (mobileTaken)
                    return UpdateFail("A user with this mobile number already exists.");
            }

            //-------------------------------------------------------
            // 4. Snapshot old values for the audit log, then apply
            //    the changes.
            //-------------------------------------------------------

            var oldValues = new
            {
                target.FullName,
                MobileNumber = target.User.MobileNumber,
                RoleName = target.Role?.RoleName,
                Permissions = SafeDeserializePermissions(target.Role?.Permissions),
                target.IsActive
            };

            var role = await ResolveRoleAsync(
                roleNameInput,
                permissions,
                fullName,
                updatedByAdminId,
                currentRoleId: target.RoleId);

            target.FullName = fullName;
            target.RoleId = role.RoleId;
            target.IsActive = request.IsActive;
            target.UpdatedAt = DateTime.UtcNow;

            target.User.MobileNumber = mobileNumber;
            target.User.CountryCode = string.IsNullOrWhiteSpace(request.CountryCode)
                ? target.User.CountryCode
                : request.CountryCode.Trim();
            target.User.AccountStatus = request.IsActive ? AccountStatus.Active : AccountStatus.Suspended;
            target.User.UpdatedAt = DateTime.UtcNow;

            // Deactivating here should also kill any sessions the
            // sub-admin currently has open.
            if (!request.IsActive)
            {
                var activeSessions = await _context.AdminSessions
                    .Where(x => x.AdminId == target.AdminId && !x.IsRevoked)
                    .ToListAsync();

                foreach (var session in activeSessions)
                {
                    session.IsRevoked = true;
                    session.LogoutAt ??= DateTime.UtcNow;
                }
            }

            //-------------------------------------------------------
            // 5. Audit log
            //-------------------------------------------------------

            var newValues = new
            {
                FullName = fullName,
                MobileNumber = mobileNumber,
                RoleName = role.RoleName,
                Permissions = permissions,
                IsActive = request.IsActive
            };

            _context.AuditLogs.Add(new AuditLog
            {
                LogId = Guid.NewGuid(),
                PerformedByAdminId = updater.AdminId,
                PerformedByName = updater.FullName,
                PerformedByRole = updater.Role?.RoleName ?? updater.AdminType,
                Module = "Sub Admin",
                Action = "Update Sub Admin",
                TargetEntityType = "AdminUser",
                TargetEntityId = target.AdminId,
                TargetEntityName = fullName,
                OldValues = JsonSerializer.Serialize(oldValues),
                NewValues = JsonSerializer.Serialize(newValues),
                Description = $"Updated sub-admin '{fullName}' ({target.User.Email}).",
                IpAddress = ipAddress,
                Success = true,
                Severity = AuditActionSeverity.Resolve("Update Sub Admin"),
                SessionId = await _context.ResolveSessionIdAsync(jwtId),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            //-------------------------------------------------------
            // 6. Response
            //-------------------------------------------------------

            return new UpdateSubAdminResponseDto
            {
                Success = true,
                Message = "Sub admin updated successfully.",

                SubAdmin = new SubAdminDto
                {
                    AdminId = target.AdminId,
                    UserId = target.UserId,
                    AdminIdentifier = target.AdminIdentifier,
                    FullName = target.FullName,
                    Email = target.User.Email ?? string.Empty,
                    MobileNumber = target.User.MobileNumber,
                    AdminType = target.AdminType,
                    RoleId = role.RoleId,
                    RoleName = role.RoleName,
                    Permissions = SubAdminPermissionsDto.FromKeyList(permissions),
                    IsActive = target.IsActive,
                    CreatedAt = target.CreatedAt
                }
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            _logger.LogError(ex, "Error while updating sub-admin {SubAdminId}.", subAdminId);

            return UpdateFail("Unable to update sub admin. Please try again.");
        }
    }

    public async Task<DeleteSubAdminResponseDto> DeleteSubAdminAsync(
        Guid subAdminId,
        Guid deletedByAdminId,
        string ipAddress,
        string? jwtId = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            //-------------------------------------------------------
            // 1. Authorization
            //-------------------------------------------------------

            var deleter = await _context.AdminUsers
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.AdminId == deletedByAdminId);

            if (deleter == null || !deleter.IsActive)
                return DeleteFail("Admin account not found or inactive.");

            var deleterIsSuperAdmin =
                string.Equals(deleter.AdminType, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(deleter.AdminType, "Admin", StringComparison.OrdinalIgnoreCase);

            if (!deleterIsSuperAdmin &&
                !RoleHasPermission(deleter.Role?.Permissions, "users"))
            {
                return DeleteFail("You do not have permission to delete sub-admins.");
            }

            if (subAdminId == deletedByAdminId)
                return DeleteFail("You cannot delete your own account.");

            //-------------------------------------------------------
            // 2. Load target — only sub-admins can be deleted here.
            //-------------------------------------------------------

            var target = await _context.AdminUsers
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.AdminId == subAdminId);

            if (target == null || target.User == null)
                return DeleteFail("Sub admin not found.");

            if (!string.Equals(target.AdminType, "SubAdmin", StringComparison.OrdinalIgnoreCase))
                return DeleteFail("Only sub-admin accounts can be deleted here.");

            if (target.User.IsDeleted)
                return DeleteFail("This sub-admin has already been deleted.");

            //-------------------------------------------------------
            // 3. Soft delete — keeps the row (and its audit-log /
            //    created-by history) intact instead of a hard DELETE,
            //    which would break the AuditLogs / AdminSessions /
            //    CreatedAdmins foreign keys pointing at this admin.
            //-------------------------------------------------------

            target.IsActive = false;
            target.UpdatedAt = DateTime.UtcNow;

            target.User.IsDeleted = true;
            target.User.DeletedAt = DateTime.UtcNow;
            target.User.AccountStatus = AccountStatus.Suspended;
            target.User.SuspensionReason = "Sub-admin account removed by admin.";
            target.User.UpdatedAt = DateTime.UtcNow;

            var activeSessions = await _context.AdminSessions
                .Where(x => x.AdminId == target.AdminId && !x.IsRevoked)
                .ToListAsync();

            foreach (var session in activeSessions)
            {
                session.IsRevoked = true;
                session.LogoutAt ??= DateTime.UtcNow;
            }

            //-------------------------------------------------------
            // 4. Audit log
            //-------------------------------------------------------

            _context.AuditLogs.Add(new AuditLog
            {
                LogId = Guid.NewGuid(),
                PerformedByAdminId = deleter.AdminId,
                PerformedByName = deleter.FullName,
                PerformedByRole = deleter.Role?.RoleName ?? deleter.AdminType,
                Module = "Sub Admin",
                Action = "Delete Sub Admin",
                TargetEntityType = "AdminUser",
                TargetEntityId = target.AdminId,
                TargetEntityName = target.FullName,
                Description = $"Deleted sub-admin '{target.FullName}' ({target.User.Email}).",
                IpAddress = ipAddress,
                Success = true,
                Severity = AuditActionSeverity.Resolve("Delete Sub Admin"),
                SessionId = await _context.ResolveSessionIdAsync(jwtId),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return new DeleteSubAdminResponseDto
            {
                Success = true,
                Message = "Sub admin deleted successfully."
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            _logger.LogError(ex, "Error while deleting sub-admin {SubAdminId}.", subAdminId);

            return DeleteFail("Unable to delete sub admin. Please try again.");
        }
    }

    public async Task<UpdateSubAdminResponseDto> SuspendSubAdminAsync(
        Guid subAdminId,
        SuspendSubAdminRequestDto request,
        Guid suspendedByAdminId,
        string ipAddress,
        string? jwtId = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            //-------------------------------------------------------
            // 1. Authorization
            //-------------------------------------------------------

            var actor = await _context.AdminUsers
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.AdminId == suspendedByAdminId);

            if (actor == null || !actor.IsActive)
                return UpdateFail("Admin account not found or inactive.");

            var actorIsSuperAdmin =
                string.Equals(actor.AdminType, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(actor.AdminType, "Admin", StringComparison.OrdinalIgnoreCase);

            if (!actorIsSuperAdmin &&
                !RoleHasPermission(actor.Role?.Permissions, "users"))
            {
                return UpdateFail("You do not have permission to suspend sub-admins.");
            }

            if (subAdminId == suspendedByAdminId)
                return UpdateFail("You cannot suspend your own account.");

            //-------------------------------------------------------
            // 2. Load target — only sub-admins can be suspended here.
            //-------------------------------------------------------

            var target = await _context.AdminUsers
                .Include(x => x.User)
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.AdminId == subAdminId);

            if (target == null || target.User == null)
                return UpdateFail("Sub admin not found.");

            if (!string.Equals(target.AdminType, "SubAdmin", StringComparison.OrdinalIgnoreCase))
                return UpdateFail("Only sub-admin accounts can be suspended here.");

            if (target.User.IsDeleted)
                return UpdateFail("This sub-admin has already been deleted.");

            if (!target.IsActive)
                return UpdateFail("This sub-admin is already suspended.");

            //-------------------------------------------------------
            // 3. Suspend — keeps the row intact (unlike delete), just
            //    blocks login: AdminAuthService.SendOtpAsync/VerifyOtpAsync
            //    both reject when IsActive is false or AccountStatus isn't
            //    Active. Any open sessions are revoked immediately too.
            //-------------------------------------------------------

            var oldValues = new { target.IsActive, target.User.AccountStatus };

            target.IsActive = false;
            target.UpdatedAt = DateTime.UtcNow;

            target.User.AccountStatus = AccountStatus.Suspended;
            target.User.SuspensionReason = string.IsNullOrWhiteSpace(request.Reason)
                ? "Suspended by admin."
                : request.Reason.Trim();
            target.User.UpdatedAt = DateTime.UtcNow;

            var activeSessions = await _context.AdminSessions
                .Where(x => x.AdminId == target.AdminId && !x.IsRevoked)
                .ToListAsync();

            foreach (var session in activeSessions)
            {
                session.IsRevoked = true;
                session.LogoutAt ??= DateTime.UtcNow;
            }

            //-------------------------------------------------------
            // 4. Audit log
            //-------------------------------------------------------

            _context.AuditLogs.Add(new AuditLog
            {
                LogId = Guid.NewGuid(),
                PerformedByAdminId = actor.AdminId,
                PerformedByName = actor.FullName,
                PerformedByRole = actor.Role?.RoleName ?? actor.AdminType,
                Module = "Sub Admin",
                Action = "Suspend Sub Admin",
                TargetEntityType = "AdminUser",
                TargetEntityId = target.AdminId,
                TargetEntityName = target.FullName,
                OldValues = JsonSerializer.Serialize(oldValues),
                NewValues = JsonSerializer.Serialize(new { IsActive = false, AccountStatus = AccountStatus.Suspended, target.User.SuspensionReason }),
                Description = $"Suspended sub-admin '{target.FullName}' ({target.User.Email}).",
                IpAddress = ipAddress,
                Success = true,
                Severity = AuditActionSeverity.Resolve("Suspend Sub Admin"),
                SessionId = await _context.ResolveSessionIdAsync(jwtId),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return new UpdateSubAdminResponseDto
            {
                Success = true,
                Message = "Sub admin suspended successfully.",

                SubAdmin = new SubAdminDto
                {
                    AdminId = target.AdminId,
                    UserId = target.UserId,
                    AdminIdentifier = target.AdminIdentifier,
                    FullName = target.FullName,
                    Email = target.User.Email ?? string.Empty,
                    MobileNumber = target.User.MobileNumber,
                    AdminType = target.AdminType,
                    RoleId = target.RoleId,
                    RoleName = target.Role?.RoleName ?? string.Empty,
                    Permissions = SubAdminPermissionsDto.FromKeyList(SafeDeserializePermissions(target.Role?.Permissions)),
                    IsActive = target.IsActive,
                    CreatedAt = target.CreatedAt,
                    LastLoginAt = target.User.LastLoginAt
                }
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            _logger.LogError(ex, "Error while suspending sub-admin {SubAdminId}.", subAdminId);

            return UpdateFail("Unable to suspend sub admin. Please try again.");
        }
    }

    public async Task<UpdateSubAdminResponseDto> ActivateSubAdminAsync(
        Guid subAdminId,
        Guid activatedByAdminId,
        string ipAddress,
        string? jwtId = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            //-------------------------------------------------------
            // 1. Authorization
            //-------------------------------------------------------

            var actor = await _context.AdminUsers
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.AdminId == activatedByAdminId);

            if (actor == null || !actor.IsActive)
                return UpdateFail("Admin account not found or inactive.");

            var actorIsSuperAdmin =
                string.Equals(actor.AdminType, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(actor.AdminType, "Admin", StringComparison.OrdinalIgnoreCase);

            if (!actorIsSuperAdmin &&
                !RoleHasPermission(actor.Role?.Permissions, "users"))
            {
                return UpdateFail("You do not have permission to activate sub-admins.");
            }

            //-------------------------------------------------------
            // 2. Load target
            //-------------------------------------------------------

            var target = await _context.AdminUsers
                .Include(x => x.User)
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.AdminId == subAdminId);

            if (target == null || target.User == null)
                return UpdateFail("Sub admin not found.");

            if (!string.Equals(target.AdminType, "SubAdmin", StringComparison.OrdinalIgnoreCase))
                return UpdateFail("Only sub-admin accounts can be activated here.");

            if (target.User.IsDeleted)
                return UpdateFail("This sub-admin has been deleted and cannot be reactivated.");

            if (target.IsActive)
                return UpdateFail("This sub-admin is already active.");

            //-------------------------------------------------------
            // 3. Reactivate
            //-------------------------------------------------------

            var oldValues = new { target.IsActive, target.User.AccountStatus };

            target.IsActive = true;
            target.UpdatedAt = DateTime.UtcNow;

            target.User.AccountStatus = AccountStatus.Active;
            target.User.SuspensionReason = null;
            target.User.UpdatedAt = DateTime.UtcNow;

            //-------------------------------------------------------
            // 4. Audit log
            //-------------------------------------------------------

            _context.AuditLogs.Add(new AuditLog
            {
                LogId = Guid.NewGuid(),
                PerformedByAdminId = actor.AdminId,
                PerformedByName = actor.FullName,
                PerformedByRole = actor.Role?.RoleName ?? actor.AdminType,
                Module = "Sub Admin",
                Action = "Activate Sub Admin",
                TargetEntityType = "AdminUser",
                TargetEntityId = target.AdminId,
                TargetEntityName = target.FullName,
                OldValues = JsonSerializer.Serialize(oldValues),
                NewValues = JsonSerializer.Serialize(new { IsActive = true, AccountStatus = AccountStatus.Active }),
                Description = $"Reactivated sub-admin '{target.FullName}' ({target.User.Email}).",
                IpAddress = ipAddress,
                Success = true,
                Severity = AuditActionSeverity.Resolve("Activate Sub Admin"),
                SessionId = await _context.ResolveSessionIdAsync(jwtId),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return new UpdateSubAdminResponseDto
            {
                Success = true,
                Message = "Sub admin activated successfully.",

                SubAdmin = new SubAdminDto
                {
                    AdminId = target.AdminId,
                    UserId = target.UserId,
                    AdminIdentifier = target.AdminIdentifier,
                    FullName = target.FullName,
                    Email = target.User.Email ?? string.Empty,
                    MobileNumber = target.User.MobileNumber,
                    AdminType = target.AdminType,
                    RoleId = target.RoleId,
                    RoleName = target.Role?.RoleName ?? string.Empty,
                    Permissions = SubAdminPermissionsDto.FromKeyList(SafeDeserializePermissions(target.Role?.Permissions)),
                    IsActive = target.IsActive,
                    CreatedAt = target.CreatedAt,
                    LastLoginAt = target.User.LastLoginAt
                }
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            _logger.LogError(ex, "Error while activating sub-admin {SubAdminId}.", subAdminId);

            return UpdateFail("Unable to activate sub admin. Please try again.");
        }
    }

    // ── Helpers ─────────────────────────────────────────────────

    // Shared by Create and Update so role resolution never drifts between
    // the two flows.
    //   - "Custom": reuses the admin's existing private custom role (if
    //     nobody else shares it) instead of leaving orphaned roles behind
    //     on every edit; otherwise creates a new uniquely-named one.
    //   - Any other name: reuses (and syncs permissions on) a shared
    //     preset role, creating it the first time it's used.
    private async Task<AdminRole> ResolveRoleAsync(
        string roleNameInput,
        List<string> permissions,
        string fullName,
        Guid actingAdminId,
        Guid? currentRoleId)
    {
        var permissionsJson = JsonSerializer.Serialize(permissions);

        if (string.Equals(roleNameInput, "Custom", StringComparison.OrdinalIgnoreCase))
        {
            if (currentRoleId.HasValue)
            {
                var currentRole = await _context.AdminRoles
                    .Include(x => x.AdminUsers)
                    .FirstOrDefaultAsync(x => x.RoleId == currentRoleId.Value);

                var isPrivateCustomRole =
                    currentRole != null &&
                    !currentRole.IsSystemRole &&
                    currentRole.RoleName.StartsWith("Custom - ", StringComparison.Ordinal) &&
                    currentRole.AdminUsers.Count <= 1;

                if (isPrivateCustomRole)
                {
                    currentRole!.Permissions = permissionsJson;
                    currentRole.UpdatedAt = DateTime.UtcNow;
                    return currentRole;
                }
            }

            var uniqueRoleName = $"Custom - {fullName}";
            var suffix = 1;

            while (await _context.AdminRoles.AnyAsync(x => x.RoleName == uniqueRoleName))
            {
                suffix++;
                uniqueRoleName = $"Custom - {fullName} ({suffix})";
            }

            var newRole = new AdminRole
            {
                RoleId = Guid.NewGuid(),
                RoleName = uniqueRoleName,
                Description = "Custom permission set.",
                Permissions = permissionsJson,
                IsSystemRole = false,
                CreatedBy = actingAdminId,
                CreatedAt = DateTime.UtcNow
            };

            _context.AdminRoles.Add(newRole);

            return newRole;
        }

        var role = await _context.AdminRoles
            .FirstOrDefaultAsync(x => x.RoleName == roleNameInput);

        if (role == null)
        {
            role = new AdminRole
            {
                RoleId = Guid.NewGuid(),
                RoleName = roleNameInput,
                Permissions = permissionsJson,
                IsSystemRole = false,
                CreatedBy = actingAdminId,
                CreatedAt = DateTime.UtcNow
            };

            _context.AdminRoles.Add(role);
        }
        else
        {
            // Keep the shared preset role's permissions in sync with
            // whatever was picked on the drawer.
            role.Permissions = permissionsJson;
            role.UpdatedAt = DateTime.UtcNow;
        }

        return role;
    }

    private static List<string> SafeDeserializePermissions(string? permissionsJson)
    {
        if (string.IsNullOrWhiteSpace(permissionsJson))
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(permissionsJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static UpdateSubAdminResponseDto UpdateFail(string message)
    {
        return new UpdateSubAdminResponseDto
        {
            Success = false,
            Message = message
        };
    }

    private static DeleteSubAdminResponseDto DeleteFail(string message)
    {
        return new DeleteSubAdminResponseDto
        {
            Success = false,
            Message = message
        };
    }

    private async Task<string> GenerateAdminIdentifierAsync()
    {
        var count = await _context.AdminUsers.CountAsync();

        var next = count + 1;

        var identifier = $"ADM-{next:D6}";

        // Guard against a rare collision (e.g. a deleted admin freed up
        // a lower number) by walking forward until it's free.
        while (await _context.AdminUsers.AnyAsync(x => x.AdminIdentifier == identifier))
        {
            next++;
            identifier = $"ADM-{next:D6}";
        }

        return identifier;
    }

    private static bool RoleHasPermission(string? permissionsJson, string key)
    {
        if (string.IsNullOrWhiteSpace(permissionsJson))
            return false;

        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(permissionsJson);

            return list != null && list.Contains(key);
        }
        catch
        {
            return false;
        }
    }

    private static CreateSubAdminResponseDto Fail(string message)
    {
        return new CreateSubAdminResponseDto
        {
            Success = false,
            Message = message
        };
    }
}