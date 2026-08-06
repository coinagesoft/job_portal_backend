using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Infrastructure.Extensions
{
    public static class HttpContextExtensions
    {
        public static string GetClientIpAddress(this HttpContext context)
        {
            // If behind a reverse proxy (Nginx/IIS/Cloudflare)
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                var ip = forwardedFor.FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(ip))
                    return ip.Split(',')[0].Trim();
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        public static string GetUserAgent(this HttpContext context)
        {
            return context.Request.Headers.UserAgent.ToString();
        }
    }
}
