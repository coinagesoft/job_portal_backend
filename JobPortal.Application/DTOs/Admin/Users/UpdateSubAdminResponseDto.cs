namespace JobPortal.Application.DTOs.Admin.Users
{
    public class UpdateSubAdminResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public SubAdminDto? SubAdmin { get; set; }
    }
}