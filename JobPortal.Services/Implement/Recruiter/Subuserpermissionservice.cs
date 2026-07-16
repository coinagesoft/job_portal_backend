using System;
using System.Linq;
using System.Threading.Tasks;
using JobPortal.Domain.Entities;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Services.Implement.Recruiter
{
    public class SubUserPermissionService : ISubUserPermissionService
    {
        private readonly AppDbContext _context;

        public SubUserPermissionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Allowed, string Message)> CheckAsync(
            Guid actionUserId,
            bool isSubUser,
            Func<EmployerSubUser, bool> requiredPermission)
        {
            if (!isSubUser)
            {
                return (true, string.Empty);
            }

            var subUser = await _context.EmployerSubUsers
                .FirstOrDefaultAsync(s => s.UserId == actionUserId);

            if (subUser == null)
            {
                return (false, "Sub-user account not found.");
            }

            if (subUser.SubUserStatus == "Deactivated")
            {
                return (false, "This sub-user account has been deactivated.");
            }

            if (!subUser.InviteAccepted)
            {
                return (false, "This sub-user has not accepted their invitation yet.");
            }

            if (!requiredPermission(subUser))
            {
                return (false, "You don't have permission to perform this action.");
            }

            return (true, string.Empty);
        }
    }
}