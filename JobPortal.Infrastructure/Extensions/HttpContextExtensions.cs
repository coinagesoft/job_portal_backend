using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Infrastructure.Extensions
{
    public static class HttpContextExtensions
    {
        // The RAW TCP peer for this connection. When the app sits behind
        // a reverse proxy (Nginx/IIS/Cloudflare/load balancer) this will
        // ALWAYS be that proxy's own IP, not the real visitor — that's
        // expected and is exactly why X-Forwarded-For exists.
        private static string RawRemoteIp(this HttpContext context) =>
            context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        // Previously this trusted whatever X-Forwarded-For value showed
        // up on ANY request, with no check on who sent it. Since nothing
        // in Program.cs validates the immediate caller is actually our
        // reverse proxy, that header is just attacker-controlled input —
        // anyone can set "X-Forwarded-For: 1.2.3.4" directly and have it
        // recorded as their IP in every audit log row.
        //
        // Fix: only trust X-Forwarded-For when the request's raw TCP
        // peer is in the configured trusted-proxy allow-list (your own
        // Nginx/load balancer, set via ReverseProxy:TrustedProxies in
        // appsettings). Anything else falls back to the raw socket IP,
        // which can't be forged by the client.
        public static string GetClientIpAddress(this HttpContext context)
        {
            var rawIp = context.RawRemoteIp();

            var trustedProxies = context.RequestServices
                .GetService(typeof(IConfiguration)) is IConfiguration config
                    ? config.GetSection("ReverseProxy:TrustedProxies").Get<string[]>() ?? Array.Empty<string>()
                    : Array.Empty<string>();

            var isFromTrustedProxy =
                IPAddress.TryParse(rawIp, out var rawIpAddr) &&
                trustedProxies.Any(p =>
                    IPAddress.TryParse(p, out var trusted) &&
                    trusted.Equals(rawIpAddr));

            if (isFromTrustedProxy &&
                context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                // X-Forwarded-For can be a comma-separated hop chain
                // ("client, proxy1, proxy2"); the first entry is the
                // original client as seen by the nearest trusted proxy.
                var forwarded = forwardedFor.FirstOrDefault()?.Split(',')[0].Trim();

                if (!string.IsNullOrWhiteSpace(forwarded))
                    return forwarded;
            }

            return rawIp;
        }

        public static string GetUserAgent(this HttpContext context)
        {
            return context.Request.Headers.UserAgent.ToString();
        }
    }
}