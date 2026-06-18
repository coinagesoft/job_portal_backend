using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using JobPortal.Application.DTOs.Recruiter;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace JobPortal.Services.Implement.Recruiter
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(
            IOptions<CloudinarySettingsDto> settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Value.CloudName))
                throw new Exception("CloudName missing");

            if (string.IsNullOrWhiteSpace(settings.Value.ApiKey))
                throw new Exception("ApiKey missing");

            if (string.IsNullOrWhiteSpace(settings.Value.ApiSecret))
                throw new Exception("ApiSecret missing");

            var account = new Account(
                settings.Value.CloudName,
                settings.Value.ApiKey,
                settings.Value.ApiSecret);

            _cloudinary = new Cloudinary(account);

        }

        public async Task<CloudinaryUploadResult> UploadImageAsync(
            IFormFile file,
            string folder)
        {
            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
            {
                throw new Exception(
                    $"Cloudinary error: {result.Error.Message}");
            }

            if (result.SecureUrl == null)
            {
                throw new Exception(
                    "Cloudinary upload failed. SecureUrl is null.");
            }
            return new CloudinaryUploadResult
            {
                Url = result.SecureUrl.ToString(),
                PublicId = result.PublicId
            };
        }

        public async Task<CloudinaryUploadResult> UploadDocumentAsync(
            IFormFile file,
            string folder)
        {
            await using var stream = file.OpenReadStream();

            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder
            };

            var result = await _cloudinary.UploadAsync(uploadParams);
            if (result.Error != null)
            {
                throw new Exception(
                    $"Cloudinary error: {result.Error.Message}");
            }

            if (result.SecureUrl == null)
            {
                throw new Exception(
                    "Cloudinary upload failed. SecureUrl is null.");
            }
            return new CloudinaryUploadResult
            {
                Url = result.SecureUrl.ToString(),
                PublicId = result.PublicId
            };
        }

        public async Task DeleteAsync(string? publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
                return;

            await _cloudinary.DestroyAsync(
                new DeletionParams(publicId));
        }
    }
}