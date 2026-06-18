
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using JobPortal.Application.DTOs.Recruiter;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Npgsql.BackendMessages;
using System.Security.Principal;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(
        IOptions<CloudinarySettingsDto> settings)
    {
        var account = new Account(
            settings.Value.CloudName,
            settings.Value.ApiKey,
            settings.Value.ApiSecret);

        _cloudinary = new Cloudinary(account);
    }

    public async Task<string?> UploadImageAsync(
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

        return result.SecureUrl.ToString();
    }

    public async Task<string?> UploadDocumentAsync(
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

        return result.SecureUrl.ToString();
    }

    public async Task DeleteAsync(string publicId)
    {
        await _cloudinary.DestroyAsync(
            new DeletionParams(publicId));
    }
}