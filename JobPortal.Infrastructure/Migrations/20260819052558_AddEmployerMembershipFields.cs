using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployerMembershipFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployerDocumentRequests_AdminUsers_RequestedByAdminAdminId",
                table: "EmployerDocumentRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployerDocumentRequests_VerificationDocumentMasters_Docume~",
                table: "EmployerDocumentRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployerDocumentRequests_employer_profiles_EmployerId",
                table: "EmployerDocumentRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_payment_transactions_candidate_profiles_CandidateProfileCan~",
                table: "payment_transactions");

            migrationBuilder.DropIndex(
                name: "IX_payment_transactions_CandidateProfileCandidateId",
                table: "payment_transactions");

            migrationBuilder.DropIndex(
                name: "IX_payment_transactions_EmployerProfileEmployerId",
                table: "payment_transactions");

            migrationBuilder.DropIndex(
                name: "IX_EmployerDocumentRequests_RequestedByAdminAdminId",
                table: "EmployerDocumentRequests");

            migrationBuilder.DropColumn(
                name: "CandidateProfileCandidateId",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "EmployerProfileEmployerId",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "RequestedByAdminAdminId",
                table: "EmployerDocumentRequests");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "EmployerDocumentRequests",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "EmployerDocumentRequests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "DocumentTypeId",
                table: "EmployerDocumentRequests",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<bool>(
                name: "is_member",
                table: "employer_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "membership_plan_id",
                table: "employer_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "membership_purchased_at",
                table: "employer_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_employer_profiles_membership_plan_id",
                table: "employer_profiles",
                column: "membership_plan_id");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployerDocumentRequests_AdminUsers_RequestedBy",
                table: "EmployerDocumentRequests",
                column: "RequestedBy",
                principalTable: "AdminUsers",
                principalColumn: "AdminId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployerDocumentRequests_VerificationDocumentMasters_Docume~",
                table: "EmployerDocumentRequests",
                column: "DocumentTypeId",
                principalTable: "VerificationDocumentMasters",
                principalColumn: "DocumentTypeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployerDocumentRequests_employer_profiles_EmployerId",
                table: "EmployerDocumentRequests",
                column: "EmployerId",
                principalTable: "employer_profiles",
                principalColumn: "employer_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payment_transactions_candidate_profiles_CandidateId",
                table: "payment_transactions",
                column: "CandidateId",
                principalTable: "candidate_profiles",
                principalColumn: "candidate_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployerDocumentRequests_AdminUsers_RequestedBy",
                table: "EmployerDocumentRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployerDocumentRequests_VerificationDocumentMasters_Docume~",
                table: "EmployerDocumentRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployerDocumentRequests_employer_profiles_EmployerId",
                table: "EmployerDocumentRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_payment_transactions_candidate_profiles_CandidateId",
                table: "payment_transactions");

            migrationBuilder.DropIndex(
                name: "IX_employer_profiles_membership_plan_id",
                table: "employer_profiles");

            migrationBuilder.DropColumn(
                name: "is_member",
                table: "employer_profiles");

            migrationBuilder.DropColumn(
                name: "membership_plan_id",
                table: "employer_profiles");

            migrationBuilder.DropColumn(
                name: "membership_purchased_at",
                table: "employer_profiles");

            migrationBuilder.AddColumn<Guid>(
                name: "CandidateProfileCandidateId",
                table: "payment_transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EmployerProfileEmployerId",
                table: "payment_transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "EmployerDocumentRequests",
                type: "text",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "EmployerDocumentRequests",
                type: "text",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "DocumentTypeId",
                table: "EmployerDocumentRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestedByAdminAdminId",
                table: "EmployerDocumentRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_CandidateProfileCandidateId",
                table: "payment_transactions",
                column: "CandidateProfileCandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_EmployerProfileEmployerId",
                table: "payment_transactions",
                column: "EmployerProfileEmployerId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployerDocumentRequests_RequestedByAdminAdminId",
                table: "EmployerDocumentRequests",
                column: "RequestedByAdminAdminId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployerDocumentRequests_AdminUsers_RequestedByAdminAdminId",
                table: "EmployerDocumentRequests",
                column: "RequestedByAdminAdminId",
                principalTable: "AdminUsers",
                principalColumn: "AdminId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployerDocumentRequests_VerificationDocumentMasters_Docume~",
                table: "EmployerDocumentRequests",
                column: "DocumentTypeId",
                principalTable: "VerificationDocumentMasters",
                principalColumn: "DocumentTypeId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployerDocumentRequests_employer_profiles_EmployerId",
                table: "EmployerDocumentRequests",
                column: "EmployerId",
                principalTable: "employer_profiles",
                principalColumn: "employer_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_payment_transactions_candidate_profiles_CandidateProfileCan~",
                table: "payment_transactions",
                column: "CandidateProfileCandidateId",
                principalTable: "candidate_profiles",
                principalColumn: "candidate_id");
        }
    }
}
