using JobPortal.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IAdmin
{
    public interface ITradeHierarchyImportService
    {
        Task<TradeHierarchyImportResultDto> ImportAsync(string filePath);
    }
}
