using JobPortal.Application.DTOs.Candidate;
using Microsoft.AspNetCore.Http;

namespace JobPortal.Services.IImplement.IUploads;

public interface IFileStorageService
{
    Task<FileUploadResult> SaveFileAsync(
        IFormFile file,
        string folderName);

    Task DeleteFileAsync(string? publicId);
}