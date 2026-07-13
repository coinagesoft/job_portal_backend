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

        Task DeleteAsync(string? filePath);
    }
}