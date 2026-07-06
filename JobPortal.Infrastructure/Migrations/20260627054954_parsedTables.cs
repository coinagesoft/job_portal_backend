using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class parsedTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ParsedSkills",
                table: "candidate_cv",
                newName: "ParsedWorkHistoryJson");

            migrationBuilder.RenameColumn(
                name: "CvS3Url",
                table: "candidate_cv",
                newName: "ParsedSummary");

            migrationBuilder.AddColumn<DateOnly>(
                name: "AiExpiryDate",
                table: "passport_verifications",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiExtractedNationality",
                table: "passport_verifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiExtractedPassportNumber",
                table: "passport_verifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BackPublicId",
                table: "passport_verifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FrontPublicId",
                table: "passport_verifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ImportedAt",
                table: "passport_verifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsImportedToProfile",
                table: "passport_verifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "passport_verifications",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "AiExtractedDocumentNumber",
                table: "kyc_verifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiExtractedGender",
                table: "kyc_verifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdBackPublicId",
                table: "kyc_verifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdFrontPublicId",
                table: "kyc_verifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ImportedAt",
                table: "kyc_verifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsImportedToProfile",
                table: "kyc_verifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "kyc_verifications",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CertificatePublicId",
                table: "candidate_education",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "GeneratedAt",
                table: "candidate_cv",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "candidate_cv",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CvPublicId",
                table: "candidate_cv",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ImportedAt",
                table: "candidate_cv",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsImportedToProfile",
                table: "candidate_cv",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ParsedCertificatesJson",
                table: "candidate_cv",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParsedCity",
                table: "candidate_cv",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParsedCountry",
                table: "candidate_cv",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParsedEducationJson",
                table: "candidate_cv",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParsedLanguagesJson",
                table: "candidate_cv",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParsedProjectsJson",
                table: "candidate_cv",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParsedRawJson",
                table: "candidate_cv",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParsedSkillsJson",
                table: "candidate_cv",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParsedState",
                table: "candidate_cv",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "candidate_cv",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiExpiryDate",
                table: "passport_verifications");

            migrationBuilder.DropColumn(
                name: "AiExtractedNationality",
                table: "passport_verifications");

            migrationBuilder.DropColumn(
                name: "AiExtractedPassportNumber",
                table: "passport_verifications");

            migrationBuilder.DropColumn(
                name: "BackPublicId",
                table: "passport_verifications");

            migrationBuilder.DropColumn(
                name: "FrontPublicId",
                table: "passport_verifications");

            migrationBuilder.DropColumn(
                name: "ImportedAt",
                table: "passport_verifications");

            migrationBuilder.DropColumn(
                name: "IsImportedToProfile",
                table: "passport_verifications");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "passport_verifications");

            migrationBuilder.DropColumn(
                name: "AiExtractedDocumentNumber",
                table: "kyc_verifications");

            migrationBuilder.DropColumn(
                name: "AiExtractedGender",
                table: "kyc_verifications");

            migrationBuilder.DropColumn(
                name: "IdBackPublicId",
                table: "kyc_verifications");

            migrationBuilder.DropColumn(
                name: "IdFrontPublicId",
                table: "kyc_verifications");

            migrationBuilder.DropColumn(
                name: "ImportedAt",
                table: "kyc_verifications");

            migrationBuilder.DropColumn(
                name: "IsImportedToProfile",
                table: "kyc_verifications");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "kyc_verifications");

            migrationBuilder.DropColumn(
                name: "CertificatePublicId",
                table: "candidate_education");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "candidate_cv");

            migrationBuilder.DropColumn(
                name: "CvPublicId",
                table: "candidate_cv");

            migrationBuilder.DropColumn(
                name: "ImportedAt",
                table: "candidate_cv");

            migrationBuilder.DropColumn(
                name: "IsImportedToProfile",
                table: "candidate_cv");

            migrationBuilder.DropColumn(
                name: "ParsedCertificatesJson",
                table: "candidate_cv");

            migrationBuilder.DropColumn(
                name: "ParsedCity",
                table: "candidate_cv");

            migrationBuilder.DropColumn(
                name: "ParsedCountry",
                table: "candidate_cv");

            migrationBuilder.DropColumn(
                name: "ParsedEducationJson",
                table: "candidate_cv");

            migrationBuilder.DropColumn(
                name: "ParsedLanguagesJson",
                table: "candidate_cv");

            migrationBuilder.DropColumn(
                name: "ParsedProjectsJson",
                table: "candidate_cv");

            migrationBuilder.DropColumn(
                name: "ParsedRawJson",
                table: "candidate_cv");

            migrationBuilder.DropColumn(
                name: "ParsedSkillsJson",
                table: "candidate_cv");

            migrationBuilder.DropColumn(
                name: "ParsedState",
                table: "candidate_cv");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "candidate_cv");

            migrationBuilder.RenameColumn(
                name: "ParsedWorkHistoryJson",
                table: "candidate_cv",
                newName: "ParsedSkills");

            migrationBuilder.RenameColumn(
                name: "ParsedSummary",
                table: "candidate_cv",
                newName: "CvS3Url");

            migrationBuilder.AlterColumn<DateTime>(
                name: "GeneratedAt",
                table: "candidate_cv",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }
    }
}
