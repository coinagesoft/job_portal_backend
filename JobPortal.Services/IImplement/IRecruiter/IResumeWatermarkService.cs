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
        /// <param name="downloadedByLabel">
        ///   Shown on the watermark as "Downloaded by: {label}" — the
        ///   employer's company name for an employer download, or the
        ///   candidate's own name for a self-download.
        /// </param>
        /// <param name="referenceId">
        ///   Shown on the watermark as a short reference ID — the
        ///   employer's or candidate's GUID, whichever is relevant.
        /// </param>
        /// <param name="downloadedAt">Timestamp shown on the watermark.</param>
        /// <returns>In-memory watermarked PDF bytes ready to stream.</returns>
        Task<byte[]> AddWatermarkAsync(
            string cvFileUrl,
            string downloadedByLabel,
            Guid referenceId,
            DateTime downloadedAt);
    }
}