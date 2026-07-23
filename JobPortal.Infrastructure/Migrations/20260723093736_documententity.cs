using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class documententity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AiConfidenceScore",
                table: "EmployerVerificationDocuments",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DetectedDocumentType",
                table: "EmployerVerificationDocuments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParsedDataJson",
                table: "EmployerVerificationDocuments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "employer_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VerifiedBy",
                table: "employer_profiles",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiConfidenceScore",
                table: "EmployerVerificationDocuments");

            migrationBuilder.DropColumn(
                name: "DetectedDocumentType",
                table: "EmployerVerificationDocuments");

            migrationBuilder.DropColumn(
                name: "ParsedDataJson",
                table: "EmployerVerificationDocuments");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "employer_profiles");

            migrationBuilder.DropColumn(
                name: "VerifiedBy",
                table: "employer_profiles");
        }
    }
}
