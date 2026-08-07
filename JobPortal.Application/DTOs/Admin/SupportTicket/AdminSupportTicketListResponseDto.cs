namespace JobPortal.Application.DTOs.Admin.SupportTicket
{
    public class AdminSupportTicketListResponseDto
    {
        public bool Success { get; set; } = true;

        public string? Message { get; set; }

        public List<AdminSupportTicketListItemDto> Items { get; set; } = new();

        public int TotalCount { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalPages { get; set; }
    }
}