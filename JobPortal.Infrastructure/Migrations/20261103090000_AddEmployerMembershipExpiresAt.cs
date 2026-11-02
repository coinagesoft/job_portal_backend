using System;
using JobPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <summary>
    /// Stores when a recruiter's membership ends. Credit top-ups expire with
    /// the membership, and an expired membership must be renewed before more
    /// credits can be bought.
    ///
    /// Back-fills existing members from MembershipPurchasedAt + the plan's
    /// ValidityMonths (only when the plan has a validity configured).
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261103090000_AddEmployerMembershipExpiresAt")]
    public partial class AddEmployerMembershipExpiresAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "membership_expires_at",
                table: "employer_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE employer_profiles e
                SET membership_expires_at =
                    e.membership_purchased_at + (mp.""ValidityMonths"" * INTERVAL '1 month')
                FROM ""MembershipPlans"" mp
                WHERE e.membership_plan_id = mp.""PlanId""
                  AND e.is_member = TRUE
                  AND e.membership_purchased_at IS NOT NULL
                  AND mp.""ValidityMonths"" > 0;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "membership_expires_at",
                table: "employer_profiles");
        }
    }
}