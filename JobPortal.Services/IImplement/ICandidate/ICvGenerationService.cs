using JobPortal.Application.DTOs.Candidate;
using JobPortal.Application.DTOs.Recruiter.CreditWallet;

namespace JobPortal.Services.IImplement.ICandidate
{
    public interface ICvGenerationService
    {
        /// <summary>
        /// Builds a fresh PDF from the candidate's current profile data
        /// (personal details, work history, education, skills, languages)
        /// using a fixed portal template, stores it, and records it as the
        /// candidate's "Portal CV" — separate from any originally uploaded
        /// resume, which is left untouched. Regenerating replaces the
        /// previous Portal CV file.
        /// </summary>
        Task<GenerateCvResponseDto> GenerateCvAsync(Guid candidateId);

        /// <summary>
        /// Lets a candidate download their own Portal CV with the same
        /// watermark treatment employers get (their own name instead of a
        /// company name). Fails if no Portal CV has been generated yet.
        /// </summary>
        Task<WatermarkedCvResult> DownloadOwnGeneratedCvAsync(Guid candidateId);
    }
}