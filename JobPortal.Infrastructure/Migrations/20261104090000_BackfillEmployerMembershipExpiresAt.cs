using JobPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <summary>
    /// Second back-fill for employer_profiles.membership_expires_at.
    ///
    /// The first back-fill (AddEmployerMembershipExpiresAt) only covered
    /// members that have membership_purchased_at set. Members without it were
    /// left with a NULL end date, which the app treats as "never expires".
    /// Here the purchase date falls back to the profile's created_at (the
    /// membership is bought during registration) so those members get a real
    /// end date too.
    ///
    /// A NULL end date is still correct, and intentionally left alone, for
    /// members whose plan has no validity period (ValidityMonths = 0, i.e. a
    /// lifetime membership with no credits).
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261104090000_BackfillEmployerMembershipExpiresAt")]
    public partial class BackfillEmployerMembershipExpiresAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE employer_profiles e
                SET membership_expires_at =
                    COALESCE(e.membership_purchased_at, e.created_at)
                        + (mp.""ValidityMonths"" * INTERVAL '1 month')
                FROM ""MembershipPlans"" mp
                WHERE e.membership_plan_id = mp.""PlanId""
                  AND e.is_member = TRUE
                  AND e.membership_expires_at IS NULL
                  AND mp.""ValidityMonths"" > 0;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data-only back-fill; nothing to undo.
        }
    }
}