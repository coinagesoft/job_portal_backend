namespace JobPortal.Application.DTOs.Admin.SupportTicket
{
    // Backs the "Candidates N / Recruiters N" tab counters on
    // /admin/helpAndsupport, plus a status breakdown for each side.
    // GET /api/admin/support-tickets/summary
    public class AdminSupportTicketSummaryDto
    {
        public int CandidateTotal { get; set; }

        public int CandidateOpen { get; set; }

        public int CandidateInProgress { get; set; }

        public int CandidateResolved { get; set; }

        public int RecruiterTotal { get; set; }

        public int RecruiterOpen { get; set; }

        public int RecruiterInProgress { get; set; }

        public int RecruiterResolved { get; set; }
    }
}