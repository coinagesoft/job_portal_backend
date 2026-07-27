using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientHiringFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientName",
                table: "job_postings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsClientHiring",
                table: "job_postings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowClientName",
                table: "job_postings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientName",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "IsClientHiring",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "ShowClientName",
                table: "job_postings");
        }
    }
}
