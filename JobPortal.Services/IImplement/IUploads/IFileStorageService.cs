using Microsoft.AspNetCore.Http;

namespace JobPortal.Services.IImplement.IUploads;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(
        IFormFile file,
        string folderName);
}