using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class paramaddedunlockcredist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "unlock_credits",
                table: "trade_credit_mappings");

            migrationBuilder.AddColumn<int>(
                name: "unlock_credits",
                table: "trade_tiers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "unlock_credits",
                table: "trade_tiers");

            migrationBuilder.AddColumn<int>(
                name: "unlock_credits",
                table: "trade_credit_mappings",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
