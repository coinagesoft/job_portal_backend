using JobPortal.Application.DTOs.Candidate.Profile;
using Microsoft.AspNetCore.Http;

namespace JobPortal.Services.IImplement.ICandidate;

public interface ICandidateDocumentService
{
    Task<CandidateDocumentsResponseDto> GetAllDocumentsAsync(Guid candidateId);

    /// <summary>
    /// Unified upload: OCR-parses any document, verifies the parsed name
    /// matches the candidate's profile name, and (only on match) stores the
    /// file in Cloudinary + the parsed data in the DB, keyed by documentType.
    /// </summary>
    Task<JobPortal.Application.DTOs.Candidate.UploadDocumentResponse> UploadAndVerifyDocumentAsync(
        Guid candidateId,
        IFormFile file);

    // Resume
    Task<UploadResumeResponseDto> UploadResumeAsync(
        Guid candidateId,
        IFormFile file);

    Task<DeleteResumeResponseDto> DeleteResumeAsync(
        Guid candidateId);

    // Education Certificate
    Task<UploadEducationCertificateResponseDto> UploadEducationCertificateAsync(
        Guid candidateId,
        UploadEducationCertificateRequestDto request,
        IFormFile file);

    Task<CandidateDocumentsResponseDto> GetEducationCertificatesAsync(
        Guid candidateId);

    Task<DeleteEducationCertificateResponseDto> DeleteEducationCertificateAsync(
        Guid candidateId,
        Guid educationId);

    // Passport
    Task<UploadPassportResponseDto> UploadPassportAsync(
        Guid candidateId,
        UploadPassportRequestDto request,
        IFormFile frontImage,
        IFormFile? backImage);

    Task<DeletePassportResponseDto> DeletePassportAsync(
        Guid candidateId);

    // Aadhaar
    Task<UploadAadhaarResponseDto> UploadAadhaarAsync(
        Guid candidateId,
        UploadAadhaarRequestDto request,
        IFormFile frontImage,
        IFormFile? backImage);

    Task<DeleteAadhaarResponseDto> DeleteAadhaarAsync(
        Guid candidateId);
}