using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class jobPosting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "experience_required_years",
                table: "job_postings",
                newName: "experience_min_years");

            migrationBuilder.RenameColumn(
                name: "gstn",
                table: "employer_profiles",
                newName: "gstin");

            migrationBuilder.RenameIndex(
                name: "IX_employer_profiles_gstn",
                table: "employer_profiles",
                newName: "IX_employer_profiles_gstin");

            migrationBuilder.AddColumn<Guid>(
                name: "CandidateProfileCandidateId",
                table: "passport_verifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CandidateProfileCandidateId",
                table: "kyc_verifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<List<string>>(
                name: "screening_questions",
                table: "job_postings",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "json",
                oldNullable: true);

            migrationBuilder.AlterColumn<List<string>>(
                name: "publishing_tags",
                table: "job_postings",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "json",
                oldNullable: true);

            migrationBuilder.AlterColumn<List<string>>(
                name: "key_skills",
                table: "job_postings",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "json",
                oldNullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "benefits",
                table: "job_postings",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "department",
                table: "job_postings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "duty_hours_per_day",
                table: "job_postings",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "employment_mode",
                table: "job_postings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "employment_type",
                table: "job_postings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte>(
                name: "experience_max_years",
                table: "job_postings",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "job_postings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "job_postings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_featured",
                table: "job_postings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_urgent_hiring",
                table: "job_postings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "key_responsibilities",
                table: "job_postings",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "offshore_country",
                table: "job_postings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "onshore_country",
                table: "job_postings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "onshore_pincode",
                table: "job_postings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "paid_overtime",
                table: "job_postings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "search_keywords",
                table: "job_postings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "tags",
                table: "job_postings",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "view_count",
                table: "job_postings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "work_address_line",
                table: "job_postings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverImageUrl",
                table: "employer_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacebookUrl",
                table: "employer_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstagramUrl",
                table: "employer_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkedInUrl",
                table: "employer_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalEmployees",
                table: "employer_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_passport_verifications_CandidateProfileCandidateId",
                table: "passport_verifications",
                column: "CandidateProfileCandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_kyc_verifications_CandidateProfileCandidateId",
                table: "kyc_verifications",
                column: "CandidateProfileCandidateId");

            migrationBuilder.AddForeignKey(
                name: "FK_kyc_verifications_candidate_profiles_CandidateProfileCandid~",
                table: "kyc_verifications",
                column: "CandidateProfileCandidateId",
                principalTable: "candidate_profiles",
                principalColumn: "candidate_id");

            migrationBuilder.AddForeignKey(
                name: "FK_passport_verifications_candidate_profiles_CandidateProfileC~",
                table: "passport_verifications",
                column: "CandidateProfileCandidateId",
                principalTable: "candidate_profiles",
                principalColumn: "candidate_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_kyc_verifications_candidate_profiles_CandidateProfileCandid~",
                table: "kyc_verifications");

            migrationBuilder.DropForeignKey(
                name: "FK_passport_verifications_candidate_profiles_CandidateProfileC~",
                table: "passport_verifications");

            migrationBuilder.DropIndex(
                name: "IX_passport_verifications_CandidateProfileCandidateId",
                table: "passport_verifications");

            migrationBuilder.DropIndex(
                name: "IX_kyc_verifications_CandidateProfileCandidateId",
                table: "kyc_verifications");

            migrationBuilder.DropColumn(
                name: "CandidateProfileCandidateId",
                table: "passport_verifications");

            migrationBuilder.DropColumn(
                name: "CandidateProfileCandidateId",
                table: "kyc_verifications");

            migrationBuilder.DropColumn(
                name: "benefits",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "department",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "duty_hours_per_day",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "employment_mode",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "employment_type",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "experience_max_years",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "is_featured",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "is_urgent_hiring",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "key_responsibilities",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "offshore_country",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "onshore_country",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "onshore_pincode",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "paid_overtime",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "search_keywords",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "tags",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "view_count",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "work_address_line",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "CoverImageUrl",
                table: "employer_profiles");

            migrationBuilder.DropColumn(
                name: "FacebookUrl",
                table: "employer_profiles");

            migrationBuilder.DropColumn(
                name: "InstagramUrl",
                table: "employer_profiles");

            migrationBuilder.DropColumn(
                name: "LinkedInUrl",
                table: "employer_profiles");

            migrationBuilder.DropColumn(
                name: "TotalEmployees",
                table: "employer_profiles");

            migrationBuilder.RenameColumn(
                name: "experience_min_years",
                table: "job_postings",
                newName: "experience_required_years");

            migrationBuilder.RenameColumn(
                name: "gstin",
                table: "employer_profiles",
                newName: "gstn");

            migrationBuilder.RenameIndex(
                name: "IX_employer_profiles_gstin",
                table: "employer_profiles",
                newName: "IX_employer_profiles_gstn");

            migrationBuilder.AlterColumn<string>(
                name: "screening_questions",
                table: "job_postings",
                type: "json",
                nullable: true,
                oldClrType: typeof(List<string>),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "publishing_tags",
                table: "job_postings",
                type: "json",
                nullable: true,
                oldClrType: typeof(List<string>),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "key_skills",
                table: "job_postings",
                type: "json",
                nullable: true,
                oldClrType: typeof(List<string>),
                oldType: "jsonb",
                oldNullable: true);
        }
    }
}
