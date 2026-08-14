using JobPortal.Application.DTOs.Admin.Dashboard;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IAdmin
{
    // Backs Admin ▸ Dashboard only (https://.../admin/dashboard) — one
    // read-only method per section on the page, each independently
    // callable so the dashboard can fetch/refresh sections separately.
    public interface IAdminDashboardService
    {
        // Stats widgets: 4 primary cards + 3 secondary cards.
        Task<StatsWidgetsResponseDto> GetStatsWidgetsAsync();

        // Registration Growth line chart. range: "week" | "month" | "year".
        Task<RegistrationGrowthResponseDto> GetRegistrationGrowthAsync(string range);

        // Recruiters by Industry donut chart.
        Task<RecruitersByIndustryResponseDto> GetRecruitersByIndustryAsync();

        // Revenue & Credit Growth stacked bar chart, last N months.
        Task<RevenueCreditGrowthResponseDto> GetRevenueCreditGrowthAsync(int months);

        // Platform Overview panel (Plans / Users / Audit Logs / Legal Pages).
        Task<PlatformOverviewResponseDto> GetPlatformOverviewAsync();

        // Recent Registrations table.
        Task<List<RecentRegistrationDto>> GetRecentRegistrationsAsync(int limit);

        // Recent Support Tickets table.
        Task<List<RecentSupportTicketDto>> GetRecentSupportTicketsAsync(int limit);

        // Recent Payments table.
        Task<List<RecentPaymentDto>> GetRecentPaymentsAsync(int limit);
    }
}