using JobPortal.Application.DTOs.Admin.Users;

namespace JobPortal.Services.IImplement.IAdmin
{
    public interface IAdminUserService
    {
        /// <summary>
        /// Lists sub-admin accounts (AdminType == "SubAdmin", excluding
        /// soft-deleted ones) with search/status filtering and pagination,
        /// plus unfiltered counts for the stat cards. Backs the table on
        /// /admin/users. Read-only — no audit log entry is written.
        /// </summary>
        Task<SubAdminListResponseDto> GetSubAdminsAsync(
            SubAdminListRequestDto request);

        /// <summary>
        /// Creates a new sub-admin account (User + AdminUser + AdminRole
        /// resolution) and logs the action. Only a Super Admin — or a
        /// sub-admin whose role grants the "users" sidebar tab — may call
        /// this.
        /// </summary>
        Task<CreateSubAdminResponseDto> CreateSubAdminAsync(
            CreateSubAdminRequestDto request,
            Guid createdByAdminId,
            string ipAddress);

        /// <summary>
        /// Updates an existing sub-admin's name, contact number, role and
        /// permissions, and active status, and logs the action. Only a
        /// Super Admin — or a sub-admin whose role grants the "users" sidebar
        /// tab — may call this.
        /// </summary>
        Task<UpdateSubAdminResponseDto> UpdateSubAdminAsync(
            Guid subAdminId,
            UpdateSubAdminRequestDto request,
            Guid updatedByAdminId,
            string ipAddress);

        /// <summary>
        /// Soft-deletes a sub-admin account (revokes sessions, marks the
        /// underlying user deleted) and logs the action. Only a Super
        /// Admin — or a sub-admin whose role grants the "users" sidebar tab
        /// — may call this. A sub-admin cannot delete themselves.
        /// </summary>
        Task<DeleteSubAdminResponseDto> DeleteSubAdminAsync(
            Guid subAdminId,
            Guid deletedByAdminId,
            string ipAddress);
    }
}