using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class rmparam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessRegDocPublicId",
                table: "employer_profiles");

            migrationBuilder.DropColumn(
                name: "GstCertificatePublicId",
                table: "employer_profiles");

            migrationBuilder.DropColumn(
                name: "GstCertificateUrl",
                table: "employer_profiles");

            migrationBuilder.DropColumn(
                name: "PanCardPublicId",
                table: "employer_profiles");

            migrationBuilder.DropColumn(
                name: "PanCardUrl",
                table: "employer_profiles");

            migrationBuilder.DropColumn(
                name: "PoeLicencePublicId",
                table: "employer_profiles");

            migrationBuilder.DropColumn(
                name: "RpslLicencePublicId",
                table: "employer_profiles");

            migrationBuilder.DropColumn(
                name: "business_reg_doc_url",
                table: "employer_profiles");

            migrationBuilder.DropColumn(
                name: "poe_company_name",
                table: "employer_profiles");

            migrationBuilder.DropColumn(
                name: "poe_expired_flag",
                table: "employer_profiles");

            migrationBuilder.DropColumn(
                name: "poe_licence_number",
                table: "employer_profiles");

            migrationBuilder.DropColumn(
                name: "poe_licence_s3_url",
                table: "employer_profiles");

            migrationBuilder.DropColumn(
                name: "poe_validity_date",
                table: "employer_profiles");

            migrationBuilder.DropColumn(
                name: "rpsl_company_name",
                table: "employer_profiles");

            migrationBuilder.DropColumn(
                name: "rpsl_expired_flag",
                table: "employer_profiles");

            migrationBuilder.DropColumn(
                name: "rpsl_licence_number",
                table: "employer_profiles");

            migrationBuilder.DropColumn(
                name: "rpsl_licence_s3_url",
                table: "employer_profiles");

            migrationBuilder.DropColumn(
                name: "rpsl_validity_date",
                table: "employer_profiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessRegDocPublicId",
                table: "employer_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GstCertificatePublicId",
                table: "employer_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GstCertificateUrl",
                table: "employer_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PanCardPublicId",
                table: "employer_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PanCardUrl",
                table: "employer_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PoeLicencePublicId",
                table: "employer_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RpslLicencePublicId",
                table: "employer_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "business_reg_doc_url",
                table: "employer_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "poe_company_name",
                table: "employer_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "poe_expired_flag",
                table: "employer_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "poe_licence_number",
                table: "employer_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "poe_licence_s3_url",
                table: "employer_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "poe_validity_date",
                table: "employer_profiles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rpsl_company_name",
                table: "employer_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "rpsl_expired_flag",
                table: "employer_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "rpsl_licence_number",
                table: "employer_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rpsl_licence_s3_url",
                table: "employer_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "rpsl_validity_date",
                table: "employer_profiles",
                type: "date",
                nullable: true);
        }
    }
}
