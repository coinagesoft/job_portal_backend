using JobPortal.Application.DTOs.Candidate;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.ICandidate;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.Implement.Candidate
{
    public class PublicCompanyService : IPublicCompanyService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PublicCompanyService> _logger;

        public PublicCompanyService(
            AppDbContext context,
            ILogger<PublicCompanyService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PublicCompanyListResponseDto> GetCompaniesAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<PublicCompanyDetailResponseDto> GetCompanyDetailAsync(Guid employerId)
        {
            throw new NotImplementedException();
        }
    }
}
