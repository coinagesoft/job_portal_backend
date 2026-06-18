using JobPortal.Application.DTOs.Recruiter;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IRecruiter
{
    public interface ICloudinaryService
    {
        Task<CloudinaryUploadResult>
        UploadImageAsync(
            IFormFile file,
            string folder);

        Task<CloudinaryUploadResult>
            UploadDocumentAsync(
                IFormFile file,
                string folder);

        Task DeleteAsync(string? publicId);
    }
}
