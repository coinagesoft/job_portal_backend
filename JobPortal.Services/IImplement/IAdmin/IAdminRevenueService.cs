using JobPortal.Application.DTOs.Admin.Revenue;
using System;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IAdmin
{
    // Backs Admin ▸ Revenue (https://.../admin/revenue) only.
    public interface IAdminRevenueService
    {
        // Filters (country / date range / type / search / period) were
        // removed for the testing phase so the tester gets one
        // unambiguous number to reconcile against the plan → purchase →
        // membership flow. Re-add the parameters here (and in
        // AdminRevenueService) once QA sign-off is done and the filters
        // need to come back for the real admin panel.
        Task<RevenueSummaryDto> GetSummaryAsync();

        Task<RevenueByCountryDto> GetRevenueByCountryAsync();

        Task<RevenueTransactionsResponseDto> GetTransactionsAsync(
            int page,
            int pageSize);

        Task<RevenueTransactionDto?> GetTransactionInvoiceAsync(
            Guid transactionId);
    }
}