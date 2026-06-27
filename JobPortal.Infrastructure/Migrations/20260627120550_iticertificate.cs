using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class iticertificate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ImportedAt",
                table: "iti_certificate_reviews",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsImportedToProfile",
                table: "iti_certificate_reviews",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ItiCertPublicId",
                table: "iti_certificate_reviews",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "iti_certificate_reviews",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImportedAt",
                table: "iti_certificate_reviews");

            migrationBuilder.DropColumn(
                name: "IsImportedToProfile",
                table: "iti_certificate_reviews");

            migrationBuilder.DropColumn(
                name: "ItiCertPublicId",
                table: "iti_certificate_reviews");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "iti_certificate_reviews");
        }
    }
}
