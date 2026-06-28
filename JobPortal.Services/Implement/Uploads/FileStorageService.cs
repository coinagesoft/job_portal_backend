using JobPortal.Application.DTOs.Candidate;
using JobPortal.Services.IImplement.IRecruiter;
using JobPortal.Services.IImplement.IUploads;
using Microsoft.AspNetCore.Http;

namespace JobPortal.Services.Implement.Uploads;

public class FileStorageService : IFileStorageService
{
    private readonly ICloudinaryService _cloudinaryService;

    public FileStorageService(
        ICloudinaryService cloudinaryService)
    {
        _cloudinaryService = cloudinaryService;
    }

    public async Task<FileUploadResult> SaveFileAsync(
        IFormFile file,
        string folderName,
        string? fileName = null)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is required.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        var imageExtensions = new[]
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        var result = imageExtensions.Contains(extension)
            ? await _cloudinaryService.UploadImageAsync(file, folderName, fileName)
            : await _cloudinaryService.UploadDocumentAsync(file, folderName, fileName);

        return new FileUploadResult
        {
            Url = result.Url,
            PublicId = result.PublicId
        };
    }

    public async Task DeleteFileAsync(string? publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            return;

        await _cloudinaryService.DeleteAsync(publicId);
    }
}