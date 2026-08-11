using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    [Migration("20260811060000_AddRegionAndBonusToCreditPlan")]
    public partial class AddRegionAndBonusToCreditPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "CreditPlans",
                type: "text",
                nullable: false,
                defaultValue: "us");

            migrationBuilder.AddColumn<string>(
                name: "Bonus",
                table: "CreditPlans",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Region",
                table: "CreditPlans");

            migrationBuilder.DropColumn(
                name: "Bonus",
                table: "CreditPlans");
        }
    }
}