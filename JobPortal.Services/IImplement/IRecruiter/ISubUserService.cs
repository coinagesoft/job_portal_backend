using JobPortal.Application.DTOs.Recruiter;
using JobPortal.Application.DTOs.SubUser;
using JobPortal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IRecruiter
{

    public interface ISubUserService
    {
        // Get all sub-users for employer
        Task<SubUserListResponseDto> GetSubUsersAsync(Guid employerId);

        // Invite new sub-user — sends invite link
        Task<InviteSubUserResponseDto> InviteSubUserAsync(
            InviteSubUserRequestDto request, Guid employerId);

        // Edit role/permissions
        Task<InviteSubUserResponseDto> UpdateSubUserAsync(
            Guid subUserId, UpdateSubUserRequestDto request, Guid employerId);

        // Deactivate — revokes access immediately
        Task<BaseSubUserResponseDto> DeactivateSubUserAsync(
            Guid subUserId, Guid employerId);

        // Reactivate
        Task<BaseSubUserResponseDto> ReactivateSubUserAsync(
            Guid subUserId, Guid employerId);

        // Resend invite
        Task<BaseSubUserResponseDto> ResendInviteAsync(
            Guid subUserId, Guid employerId);

        // Accept invite (called by sub-user)
        Task<BaseSubUserResponseDto> AcceptInviteAsync(AcceptInviteRequestDto request);

        // Get permission matrix for a role
        PermissionsDto GetRolePermissions(SubUserRole role);

        Task<BaseSubUserResponseDto> DeleteSubUserAsync(Guid subUserId, Guid employerId);

        Task<ValidateInviteResponseDto> ValidateInviteAsync(string token);
    }

    public class BaseSubUserResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Email { get; set; }
    }
}