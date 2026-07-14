using JobPortal.Application.DTOs.Recruiter;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace JobPortal.Services.Implement.Recruiter
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LocalFileStorageService(
            IWebHostEnvironment environment,
            IHttpContextAccessor httpContextAccessor)
        {
            _environment = environment;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<FileUploadResult> UploadImageAsync(
            IFormFile file,
            string folder,
            string? publicId = null)
        {
            return await SaveFile(file, folder, publicId);
        }

        public async Task<FileUploadResult> UploadDocumentAsync(
            IFormFile file,
            string folder,
            string? publicId = null)
        {
            return await SaveFile(file, folder, publicId);
        }

        public async Task<FileUploadResult> UploadBytesAsync(
            byte[] bytes,
            string folder,
            string fileName,
            string contentType)
        {
            var uploadsRoot = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                folder);

            Directory.CreateDirectory(uploadsRoot);

            string fullPath = Path.Combine(uploadsRoot, fileName);

            await File.WriteAllBytesAsync(fullPath, bytes);

            var request = _httpContextAccessor.HttpContext!.Request;

            string fileUrl =
                $"{request.Scheme}://{request.Host}/uploads/{folder}/{fileName}";

            return new FileUploadResult
            {
                Url = fileUrl,
                PublicId = fileName
            };
        }

        private async Task<FileUploadResult> SaveFile(
            IFormFile file,
            string folder,
            string? publicId)
        {
            var uploadsRoot = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                folder);

            Directory.CreateDirectory(uploadsRoot);

            string extension = Path.GetExtension(file.FileName);

            string fileName = !string.IsNullOrWhiteSpace(publicId)
                ? publicId + extension
                : $"{Guid.NewGuid()}{extension}";

            string fullPath = Path.Combine(uploadsRoot, fileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var request = _httpContextAccessor.HttpContext!.Request;

            string fileUrl =
                $"{request.Scheme}://{request.Host}/uploads/{folder}/{fileName}";

            return new FileUploadResult
            {
                Url = fileUrl,
                PublicId = fileName
            };
        }

        public Task DeleteAsync(string? publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
                return Task.CompletedTask;

            string uploads = Path.Combine(_environment.WebRootPath, "uploads");

            var file = Directory
                .GetFiles(uploads, publicId, SearchOption.AllDirectories)
                .FirstOrDefault();

            if (file != null)
            {
                File.Delete(file);
            }

            return Task.CompletedTask;
        }
    }
}