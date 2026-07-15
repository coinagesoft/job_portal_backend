using System;
using System.Threading.Tasks;
using JobPortal.Domain.Entities;

namespace JobPortal.Services.IImplement.IRecruiter
{
    public interface ISubUserPermissionService
    {
        /// <summary>
        /// Enforces a sub-user's ACTUAL, current permission flags from the
        /// database before letting a gated action through. If isSubUser is
        /// false (the actor is the employer account owner), this always
        /// allows the action — permission flags only ever restrict
        /// sub-users, never the owner.
        /// </summary>
        Task<(bool Allowed, string Message)> CheckAsync(
            Guid actionUserId,
            bool isSubUser,
            Func<EmployerSubUser, bool> requiredPermission);
    }
}