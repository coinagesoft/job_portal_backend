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
        private const string CreditGraceDaysKey = "Membership:CreditGraceDays";
        private const int DefaultCreditGraceDays = 30;

        /// <summary>
        /// How many days after a membership expires the recruiter can still
        /// renew and get the credits they had left back. Until they renew,
        /// those credits are on hold and cannot be spent. After this many
        /// days they are forfeited. Default 30 (Membership:CreditGraceDays).
        /// </summary>
        public static int GetCreditGraceDays(IConfiguration configuration)
        {
            return int.TryParse(configuration[CreditGraceDaysKey], out var days)
                ? Math.Clamp(days, 0, 365)
                : DefaultCreditGraceDays;
        }

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