using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.Notification
{
    public class NotificationListResponseDto
    {
        public int TotalCount { get; set; }

        public int UnreadCount { get; set; }

        public List<NotificationItemDto> Notifications { get; set; } = new();
    }
}
