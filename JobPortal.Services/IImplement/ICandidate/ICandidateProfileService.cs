using JobPortal.Application.DTOs.Candidate.Profile;
using Microsoft.AspNetCore.Http;

namespace JobPortal.Services.IImplement.ICandidate;

public interface ICandidateProfileService
{
    Task<CandidateProfileSummaryResponseDto> GetProfileSummaryAsync(
        Guid candidateId);

    Task<CandidatePersonalInfoResponseDto> GetPersonalInfoAsync(
        Guid candidateId);

    Task<UpdateCandidatePersonalInfoResponseDto> UpdatePersonalInfoAsync(
        Guid candidateId,
        UpdateCandidatePersonalInfoRequestDto request);

    Task<UploadProfilePhotoResponseDto> UploadProfilePhotoAsync(
        Guid candidateId,
        IFormFile photo);

    Task<UploadProfilePhotoResponseDto> DeleteProfilePhotoAsync(
        Guid candidateId);

    Task<ProfileCompletionResponseDto> GetProfileCompletionAsync(
        Guid candidateId);

    Task<CreateCandidateProfileResponseDto> CreateProfileAsync(
    Guid userId,
    CreateCandidateProfileRequestDto request);
}