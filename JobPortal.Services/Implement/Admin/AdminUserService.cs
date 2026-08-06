using JobPortal.Application.DTOs.Admin.Users;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums.common;
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

    public async Task<CreateSubAdminResponseDto> CreateSubAdminAsync(
        CreateSubAdminRequestDto request,
        Guid createdByAdminId,
        string ipAddress)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            //-------------------------------------------------------
            // 1. Authorization — only the top-level admin ("Admin" /
            //    "SuperAdmin" — this DB's existing seed data uses
            //    "Admin"), or a sub-admin whose role explicitly grants
            //    "subadmin.create", may add new sub-admins.
            //-------------------------------------------------------

            var creator = await _context.AdminUsers
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.AdminId == createdByAdminId);

            if (creator == null || !creator.IsActive)
                return Fail("Admin account not found or inactive.");

            var creatorIsSuperAdmin =
                string.Equals(creator.AdminType, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(creator.AdminType, "Admin", StringComparison.OrdinalIgnoreCase);

            if (!creatorIsSuperAdmin &&
                !RoleHasPermission(creator.Role?.Permissions, "subadmin.create"))
            {
                return Fail("You do not have permission to create sub-admins.");
            }

            //-------------------------------------------------------
            // 2. Basic validation
            //-------------------------------------------------------

            var email = request.Email.Trim().ToLower();
            var fullName = request.FullName.Trim();
            var roleNameInput = request.RoleName.Trim();
            var permissions = (request.Permissions ?? new List<string>())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .Distinct()
                .ToList();

            if (permissions.Count == 0)
                return Fail("At least one permission must be selected.");

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

            var permissionsJson = JsonSerializer.Serialize(permissions);

            AdminRole? role;

            if (string.Equals(roleNameInput, "Custom", StringComparison.OrdinalIgnoreCase))
            {
                var uniqueRoleName = $"Custom - {fullName}";

                var suffix = 1;

                while (await _context.AdminRoles.AnyAsync(x => x.RoleName == uniqueRoleName))
                {
                    suffix++;
                    uniqueRoleName = $"Custom - {fullName} ({suffix})";
                }

                role = new AdminRole
                {
                    RoleId = Guid.NewGuid(),
                    RoleName = uniqueRoleName,
                    Description = "Custom permission set.",
                    Permissions = permissionsJson,
                    IsSystemRole = false,
                    CreatedBy = createdByAdminId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.AdminRoles.Add(role);
            }
            else
            {
                role = await _context.AdminRoles
                    .FirstOrDefaultAsync(x => x.RoleName == roleNameInput);

                if (role == null)
                {
                    role = new AdminRole
                    {
                        RoleId = Guid.NewGuid(),
                        RoleName = roleNameInput,
                        Permissions = permissionsJson,
                        IsSystemRole = false,
                        CreatedBy = createdByAdminId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.AdminRoles.Add(role);
                }
                else
                {
                    // Keep the shared preset role's permissions in sync
                    // with whatever was picked on the drawer.
                    role.Permissions = permissionsJson;
                    role.UpdatedAt = DateTime.UtcNow;
                }
            }

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
                    Permissions = permissions,
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

    // ── Helpers ─────────────────────────────────────────────────

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