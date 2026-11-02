using System;
using Microsoft.Extensions.Configuration;

namespace JobPortal.Services.Implement.Recruiter
{
    /// <summary>
    /// Settings shared by the recruiter membership / credit services.
    /// </summary>
    public static class MembershipRules
    {
        private const string EarlyRenewalWindowKey = "Membership:EarlyRenewalWindowDays";

        /// <summary>
        /// How many days before the membership end date a recruiter may renew
        /// early. 0 (the default, also used when the setting is missing or
        /// invalid) disables early renewal: a membership can only be bought
        /// again after the previous one has expired.
        /// </summary>
        public static int GetEarlyRenewalWindowDays(IConfiguration configuration)
        {
            return int.TryParse(configuration[EarlyRenewalWindowKey], out var days)
                ? Math.Clamp(days, 0, 365)
                : 0;
        }
    }
}