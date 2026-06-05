using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CandidateProfileExtended : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_passport_verifications_admin_users_ReviewedBy",
                table: "passport_verifications");

            migrationBuilder.DropForeignKey(
                name: "FK_passport_verifications_candidate_profiles_CandidateId",
                table: "passport_verifications");

            migrationBuilder.DropColumn(
                name: "AiExtractedFullName",
                table: "passport_verifications");

            migrationBuilder.DropColumn(
                name: "ExpiryAutoFlagged",
                table: "passport_verifications");

            migrationBuilder.RenameColumn(
                name: "ReviewedBy",
                table: "passport_verifications",
                newName: "reviewed_by");

            migrationBuilder.RenameColumn(
                name: "ReviewedAt",
                table: "passport_verifications",
                newName: "reviewed_at");

            migrationBuilder.RenameColumn(
                name: "RejectionReason",
                table: "passport_verifications",
                newName: "rejection_reason");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "passport_verifications",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CandidateId",
                table: "passport_verifications",
                newName: "candidate_id");

            migrationBuilder.RenameColumn(
                name: "AiConfidenceScore",
                table: "passport_verifications",
                newName: "ai_confidence_score");

            migrationBuilder.RenameColumn(
                name: "AdminDecision",
                table: "passport_verifications",
                newName: "admin_decision");

            migrationBuilder.RenameColumn(
                name: "PassportImageUrl",
                table: "passport_verifications",
                newName: "front_image_url");

            migrationBuilder.RenameColumn(
                name: "AiExtractedPassportNo",
                table: "passport_verifications",
                newName: "back_image_url");

            migrationBuilder.RenameColumn(
                name: "AiExtractedNationality",
                table: "passport_verifications",
                newName: "ai_extracted_name");

            migrationBuilder.RenameColumn(
                name: "AiExtractedExpiryDate",
                table: "passport_verifications",
                newName: "ai_extracted_dob");

            migrationBuilder.RenameColumn(
                name: "PassportVerId",
                table: "passport_verifications",
                newName: "verification_id");

            migrationBuilder.RenameIndex(
                name: "IX_passport_verifications_ReviewedBy",
                table: "passport_verifications",
                newName: "IX_passport_verifications_reviewed_by");

            migrationBuilder.RenameIndex(
                name: "IX_passport_verifications_CandidateId",
                table: "passport_verifications",
                newName: "IX_passport_verifications_candidate_id");

            migrationBuilder.AlterColumn<string>(
                name: "PlanName",
                table: "EmployerCreditPlan",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "CreditPlanPlanId",
                table: "EmployerCreditPlan",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "About",
                table: "candidate_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoticePeriod",
                table: "candidate_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pincode",
                table: "candidate_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfessionalSummary",
                table: "candidate_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployerCreditPlan_CreditPlanPlanId",
                table: "EmployerCreditPlan",
                column: "CreditPlanPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployerCreditPlan_EmployerId",
                table: "EmployerCreditPlan",
                column: "EmployerId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployerCreditPlan_CreditPlans_CreditPlanPlanId",
                table: "EmployerCreditPlan",
                column: "CreditPlanPlanId",
                principalTable: "CreditPlans",
                principalColumn: "PlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployerCreditPlan_employer_profiles_EmployerId",
                table: "EmployerCreditPlan",
                column: "EmployerId",
                principalTable: "employer_profiles",
                principalColumn: "employer_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_passport_verifications_admin_users_reviewed_by",
                table: "passport_verifications",
                column: "reviewed_by",
                principalTable: "admin_users",
                principalColumn: "admin_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_passport_verifications_candidate_profiles_candidate_id",
                table: "passport_verifications",
                column: "candidate_id",
                principalTable: "candidate_profiles",
                principalColumn: "candidate_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployerCreditPlan_CreditPlans_CreditPlanPlanId",
                table: "EmployerCreditPlan");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployerCreditPlan_employer_profiles_EmployerId",
                table: "EmployerCreditPlan");

            migrationBuilder.DropForeignKey(
                name: "FK_passport_verifications_admin_users_reviewed_by",
                table: "passport_verifications");

            migrationBuilder.DropForeignKey(
                name: "FK_passport_verifications_candidate_profiles_candidate_id",
                table: "passport_verifications");

            migrationBuilder.DropIndex(
                name: "IX_EmployerCreditPlan_CreditPlanPlanId",
                table: "EmployerCreditPlan");

            migrationBuilder.DropIndex(
                name: "IX_EmployerCreditPlan_EmployerId",
                table: "EmployerCreditPlan");

            migrationBuilder.DropColumn(
                name: "CreditPlanPlanId",
                table: "EmployerCreditPlan");

            migrationBuilder.DropColumn(
                name: "About",
                table: "candidate_profiles");

            migrationBuilder.DropColumn(
                name: "NoticePeriod",
                table: "candidate_profiles");

            migrationBuilder.DropColumn(
                name: "Pincode",
                table: "candidate_profiles");

            migrationBuilder.DropColumn(
                name: "ProfessionalSummary",
                table: "candidate_profiles");

            migrationBuilder.RenameColumn(
                name: "reviewed_by",
                table: "passport_verifications",
                newName: "ReviewedBy");

            migrationBuilder.RenameColumn(
                name: "reviewed_at",
                table: "passport_verifications",
                newName: "ReviewedAt");

            migrationBuilder.RenameColumn(
                name: "rejection_reason",
                table: "passport_verifications",
                newName: "RejectionReason");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "passport_verifications",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "candidate_id",
                table: "passport_verifications",
                newName: "CandidateId");

            migrationBuilder.RenameColumn(
                name: "ai_confidence_score",
                table: "passport_verifications",
                newName: "AiConfidenceScore");

            migrationBuilder.RenameColumn(
                name: "admin_decision",
                table: "passport_verifications",
                newName: "AdminDecision");

            migrationBuilder.RenameColumn(
                name: "front_image_url",
                table: "passport_verifications",
                newName: "PassportImageUrl");

            migrationBuilder.RenameColumn(
                name: "back_image_url",
                table: "passport_verifications",
                newName: "AiExtractedPassportNo");

            migrationBuilder.RenameColumn(
                name: "ai_extracted_name",
                table: "passport_verifications",
                newName: "AiExtractedNationality");

            migrationBuilder.RenameColumn(
                name: "ai_extracted_dob",
                table: "passport_verifications",
                newName: "AiExtractedExpiryDate");

            migrationBuilder.RenameColumn(
                name: "verification_id",
                table: "passport_verifications",
                newName: "PassportVerId");

            migrationBuilder.RenameIndex(
                name: "IX_passport_verifications_reviewed_by",
                table: "passport_verifications",
                newName: "IX_passport_verifications_ReviewedBy");

            migrationBuilder.RenameIndex(
                name: "IX_passport_verifications_candidate_id",
                table: "passport_verifications",
                newName: "IX_passport_verifications_CandidateId");

            migrationBuilder.AddColumn<string>(
                name: "AiExtractedFullName",
                table: "passport_verifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExpiryAutoFlagged",
                table: "passport_verifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "PlanName",
                table: "EmployerCreditPlan",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddForeignKey(
                name: "FK_passport_verifications_admin_users_ReviewedBy",
                table: "passport_verifications",
                column: "ReviewedBy",
                principalTable: "admin_users",
                principalColumn: "admin_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_passport_verifications_candidate_profiles_CandidateId",
                table: "passport_verifications",
                column: "CandidateId",
                principalTable: "candidate_profiles",
                principalColumn: "candidate_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
