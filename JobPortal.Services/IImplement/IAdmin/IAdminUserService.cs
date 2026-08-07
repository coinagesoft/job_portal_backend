using JobPortal.Application.DTOs.Admin.Users;

namespace JobPortal.Services.IImplement.IAdmin
{
    public interface IAdminUserService
    {
        /// <summary>
        /// Creates a new sub-admin account (User + AdminUser + AdminRole
        /// resolution) and logs the action. Only a Super Admin — or a
        /// sub-admin whose role grants "subadmin.create" — may call this.
        /// </summary>
        Task<CreateSubAdminResponseDto> CreateSubAdminAsync(
            CreateSubAdminRequestDto request,
            Guid createdByAdminId,
            string ipAddress);

        /// <summary>
        /// Updates an existing sub-admin's name, contact number, role and
        /// permissions, and active status, and logs the action. Only a
        /// Super Admin — or a sub-admin whose role grants "subadmin.edit"
        /// — may call this.
        /// </summary>
        Task<UpdateSubAdminResponseDto> UpdateSubAdminAsync(
            Guid subAdminId,
            UpdateSubAdminRequestDto request,
            Guid updatedByAdminId,
            string ipAddress);

        /// <summary>
        /// Soft-deletes a sub-admin account (revokes sessions, marks the
        /// underlying user deleted) and logs the action. Only a Super
        /// Admin — or a sub-admin whose role grants "subadmin.delete" —
        /// may call this. A sub-admin cannot delete themselves.
        /// </summary>
        Task<DeleteSubAdminResponseDto> DeleteSubAdminAsync(
            Guid subAdminId,
            Guid deletedByAdminId,
            string ipAddress);
    }
}