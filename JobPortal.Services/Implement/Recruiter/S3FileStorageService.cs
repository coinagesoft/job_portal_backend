// Put in: JobPortal.Services/Implement/Recruiter/S3FileStorageService.cs
// NuGet (JobPortal.Services project):  dotnet add package AWSSDK.S3
using Amazon.S3;
using Amazon.S3.Model;
using JobPortal.Application.DTOs.Recruiter;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Recruiter
{
    public class S3FileStorageService : IFileStorageService
    {
        private readonly IAmazonS3 _s3;
        private readonly string _bucket;
        private readonly IHttpContextAccessor _http;
        private readonly string? _publicBaseUrl;
        private readonly ILogger<S3FileStorageService> _logger;

        public S3FileStorageService(
            IAmazonS3 s3,
            IConfiguration config,
            IHttpContextAccessor http,
            ILogger<S3FileStorageService> logger)
        {
            _s3 = s3;
            _http = http;
            _logger = logger;
            _bucket = config["AWS:BucketName"]
                ?? throw new InvalidOperationException("AWS:BucketName missing");
            // Optional: set to https://api.yourdomain.com so URLs don't depend on request host
            _publicBaseUrl = config["Storage:PublicBaseUrl"]?.TrimEnd('/');
        }

        public Task<FileUploadResult> UploadImageAsync(IFormFile file, string folder, string? fileName = null)
            => SaveFormFile(file, folder, fileName);

        public Task<FileUploadResult> UploadDocumentAsync(IFormFile file, string folder, string? fileName = null)
            => SaveFormFile(file, folder, fileName);

        private async Task<FileUploadResult> SaveFormFile(IFormFile file, string folder, string? publicId)
        {
            var ext = Path.GetExtension(file.FileName);
            var name = !string.IsNullOrWhiteSpace(publicId) ? publicId + ext : $"{Guid.NewGuid()}{ext}";
            var key = BuildKey(folder, name);

            await using var stream = file.OpenReadStream();
            await _s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = stream,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                    ? "application/octet-stream" : file.ContentType,
                ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
            });

            return Result(key);
        }

        public async Task<FileUploadResult> UploadBytesAsync(
            byte[] bytes, string folder, string fileName, string contentType)
        {
            var key = BuildKey(folder, fileName);
            using var ms = new MemoryStream(bytes);
            await _s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = ms,
                ContentType = contentType,
                ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
            });
            return Result(key);
        }

        // publicId can be:
        //  - a full key  "resumes/abc.pdf"   (all NEW uploads)
        //  - only a file "abc.pdf"           (OLD rows already in your DB) -> we search for it
        public async Task DeleteAsync(string? publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId)) return;
            try
            {
                var key = publicId.Contains('/') ? publicId : await FindKeyByFileName(publicId);
                if (key == null) return;
                await _s3.DeleteObjectAsync(_bucket, key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "S3 delete failed for {PublicId}", publicId);
            }
        }

        // Used by the /uploads/... controller and the watermark service
        public async Task<(Stream Stream, string ContentType)?> GetAsync(string key)
        {
            try
            {
                var resp = await _s3.GetObjectAsync(_bucket, key);
                return (resp.ResponseStream, resp.Headers.ContentType ?? "application/octet-stream");
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound
                                            || ex.ErrorCode == "NoSuchKey")
            {
                return null;
            }
        }

        public async Task<byte[]?> GetBytesAsync(string keyOrFileName)
        {
            var key = keyOrFileName.Contains('/') ? keyOrFileName : await FindKeyByFileName(keyOrFileName);
            if (key == null) return null;
            var obj = await GetAsync(key);
            if (obj == null) return null;
            using var s = obj.Value.Stream;
            using var ms = new MemoryStream();
            await s.CopyToAsync(ms);
            return ms.ToArray();
        }

        private async Task<string?> FindKeyByFileName(string fileName)
        {
            string? token = null;
            do
            {
                var r = await _s3.ListObjectsV2Async(new ListObjectsV2Request
                { BucketName = _bucket, ContinuationToken = token });
                var hit = r.S3Objects?.FirstOrDefault(o =>
                    o.Key != null && o.Key.EndsWith("/" + fileName, StringComparison.Ordinal));
                if (hit != null) return hit.Key;
                token = r.IsTruncated == true ? r.NextContinuationToken : null;
            } while (token != null);
            return null;
        }

        private static string BuildKey(string folder, string fileName)
            => $"{folder.Trim('/')}/{fileName}".Replace("\\", "/");

        // URL format stays EXACTLY the same as before: https://host/uploads/<key>
        // so no database change and no frontend change are needed.
        private FileUploadResult Result(string key)
        {
            string baseUrl = _publicBaseUrl
                ?? $"{_http.HttpContext!.Request.Scheme}://{_http.HttpContext.Request.Host}";
            return new FileUploadResult { Url = $"{baseUrl}/uploads/{key}", PublicId = key };
        }
    }
}