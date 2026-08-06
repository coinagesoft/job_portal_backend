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
    }
}