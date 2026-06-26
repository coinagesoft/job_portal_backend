// ============================================================
//  JobPortal.Services/IImplement/IRecruiter/
//  IResumeWatermarkService.cs
// ============================================================

namespace JobPortal.Services.IImplement.IRecruiter
{
    public interface IResumeWatermarkService
    {
        /// <summary>
        /// Downloads the original resume from local storage / URL,
        /// overlays a dynamic watermark in memory, and returns the
        /// watermarked PDF bytes.  The original file is never modified.
        /// </summary>
        /// <param name="cvFileUrl">
        ///   Relative path stored in CandidateCv.CvFileUrl
        ///   (e.g. "resumes/abc.pdf") or an absolute URL.
        /// </param>
        /// <param name="employerName">Company name shown on the watermark.</param>
        /// <param name="employerId">Employer GUID shown on the watermark.</param>
        /// <param name="downloadedAt">Timestamp shown on the watermark.</param>
        /// <returns>In-memory watermarked PDF bytes ready to stream.</returns>
        Task<byte[]> AddWatermarkAsync(
            string cvFileUrl,
            string employerName,
            Guid employerId,
            DateTime downloadedAt);
    }
}