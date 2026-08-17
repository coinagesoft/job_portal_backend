using JobPortal.Application.DTOs.Admin.Revenue;
using System;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IAdmin
{
    // Backs Admin ▸ Revenue (https://.../admin/revenue) only.
    public interface IAdminRevenueService
    {
        Task<RevenueSummaryDto> GetSummaryAsync(
            string? country,
            DateOnly? dateFrom,
            DateOnly? dateTo);

        Task<RevenueByCountryDto> GetRevenueByCountryAsync(
            string period,
            string? country);

        Task<RevenueTransactionsResponseDto> GetTransactionsAsync(
            string type,
            string? country,
            string? search,
            DateOnly? dateFrom,
            DateOnly? dateTo,
            int page,
            int pageSize);

        Task<RevenueTransactionDto?> GetTransactionInvoiceAsync(
            Guid transactionId);
    }
}