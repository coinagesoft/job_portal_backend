using JobPortal.Application.DTOs.Recruiter.TradeCredit;
using JobPortal.Application.DTOs.TradeCredit;
using JobPortal.Domain.Entities;
using ClosedXML.Excel;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IPublic
{
    public interface ITradeCreditImportService
    {
        Task<int> ImportExcelAsync(
     Stream excelStream,
     CancellationToken cancellationToken = default);

        Task<List<TradeCreditMappingResponseDto>> GetMappingsAsync(
    CancellationToken cancellationToken = default);

        Task<List<TradeTier>> GetTiersAsync(
    CancellationToken cancellationToken = default);
    }
}
