using JobPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <summary>
    /// Adds how long (in months) the credits granted by a recruiter
    /// membership plan remain valid. Used together with
    /// MembershipPlans.InitialCredits when the plan is purchased at
    /// registration.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261102090000_AddMembershipPlanValidityMonths")]
    public partial class AddMembershipPlanValidityMonths : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ValidityMonths",
                table: "MembershipPlans",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ValidityMonths",
                table: "MembershipPlans");
        }
    }
}