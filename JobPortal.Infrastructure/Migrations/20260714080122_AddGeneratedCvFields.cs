using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGeneratedCvFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GeneratedCvFileUrl",
                table: "candidate_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeneratedCvPublicId",
                table: "candidate_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GeneratedCvUpdatedAt",
                table: "candidate_profiles",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeneratedCvFileUrl",
                table: "candidate_profiles");

            migrationBuilder.DropColumn(
                name: "GeneratedCvPublicId",
                table: "candidate_profiles");

            migrationBuilder.DropColumn(
                name: "GeneratedCvUpdatedAt",
                table: "candidate_profiles");
        }
    }
}
