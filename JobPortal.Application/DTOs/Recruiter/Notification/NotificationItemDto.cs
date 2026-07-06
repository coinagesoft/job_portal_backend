using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.Notification
{
    public class NotificationItemDto
    {
        public Guid NotificationId { get; set; }

        public string NotificationType { get; set; } = default!;

        public string Title { get; set; } = default!;

        public string Body { get; set; } = default!;

        public bool IsRead { get; set; }

        public DateTime SentAt { get; set; }
    }
}
