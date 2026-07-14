using JobPortal.Application.DTOs.Recruiter;
using Microsoft.AspNetCore.Http;

namespace JobPortal.Services.IImplement.IRecruiter
{
    public interface IFileStorageService
    {
        Task<FileUploadResult> UploadImageAsync(
            IFormFile file,
            string folder,
            string? fileName = null);

        Task<FileUploadResult> UploadDocumentAsync(
            IFormFile file,
            string folder,
            string? fileName = null);

        /// <summary>
        /// Saves raw bytes (e.g. a programmatically-generated PDF) rather
        /// than an uploaded IFormFile. Used for the portal-generated CV.
        /// </summary>
        Task<FileUploadResult> UploadBytesAsync(
            byte[] bytes,
            string folder,
            string fileName,
            string contentType);

        Task DeleteAsync(string? filePath);
    }
}