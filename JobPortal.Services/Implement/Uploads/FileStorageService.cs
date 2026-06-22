using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
namespace JobPortal.Services.Implement.Uploads;

using JobPortal.Services.IImplement.IUploads;

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public FileStorageService(
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _environment = environment;
        _configuration = configuration;
    }

    public async Task<string> SaveFileAsync(
        IFormFile file,
        string folderName)
    {
        var uploadPath = Path.Combine(
            _environment.WebRootPath,
            "uploads",
            folderName);

        if (!Directory.Exists(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
        }

        var fileName =
            $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

        var fullPath =
            Path.Combine(uploadPath, fileName);

        using var stream =
            new FileStream(fullPath, FileMode.Create);

        await file.CopyToAsync(stream);

        var baseUrl =
            _configuration["Storage:BaseUrl"];

        return $"{baseUrl}/uploads/{folderName}/{fileName}";
    }
}