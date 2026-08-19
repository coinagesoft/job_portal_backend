using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPaymentTransactionForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payment_transactions_candidate_profiles_CandidateProfileCan~",
                table: "payment_transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_payment_transactions_employer_profiles_EmployerProfileEmplo~",
                table: "payment_transactions");

            migrationBuilder.DropIndex(
                name: "IX_payment_transactions_CandidateProfileCandidateId",
                table: "payment_transactions");

            migrationBuilder.DropIndex(
                name: "IX_payment_transactions_EmployerProfileEmployerId",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "CandidateProfileCandidateId",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "EmployerProfileEmployerId",
                table: "payment_transactions");

            // NOTE: is_member / membership_plan_id / membership_purchased_at
            // on candidate_profiles are intentionally NOT added here. They
            // were already added by the earlier migration
            // 20260812090000_AddCandidateMembershipFields. This migration
            // originally duplicated those AddColumn calls (scaffolded
            // against a stale model), which fails with
            // "42701: column already exists" once AddCandidateMembershipFields
            // has been applied. See that migration for the actual column
            // definitions.

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_CandidateId",
                table: "payment_transactions",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_EmployerId",
                table: "payment_transactions",
                column: "EmployerId");

            migrationBuilder.AddForeignKey(
                name: "FK_payment_transactions_candidate_profiles_CandidateId",
                table: "payment_transactions",
                column: "CandidateId",
                principalTable: "candidate_profiles",
                principalColumn: "candidate_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payment_transactions_employer_profiles_EmployerId",
                table: "payment_transactions",
                column: "EmployerId",
                principalTable: "employer_profiles",
                principalColumn: "employer_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payment_transactions_candidate_profiles_CandidateId",
                table: "payment_transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_payment_transactions_employer_profiles_EmployerId",
                table: "payment_transactions");

            migrationBuilder.DropIndex(
                name: "IX_payment_transactions_CandidateId",
                table: "payment_transactions");

            migrationBuilder.DropIndex(
                name: "IX_payment_transactions_EmployerId",
                table: "payment_transactions");

            // is_member / membership_plan_id / membership_purchased_at are
            // owned by 20260812090000_AddCandidateMembershipFields — not
            // dropped here (see matching note in Up()).

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

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_CandidateProfileCandidateId",
                table: "payment_transactions",
                column: "CandidateProfileCandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_EmployerProfileEmployerId",
                table: "payment_transactions",
                column: "EmployerProfileEmployerId");

            migrationBuilder.AddForeignKey(
                name: "FK_payment_transactions_candidate_profiles_CandidateProfileCan~",
                table: "payment_transactions",
                column: "CandidateProfileCandidateId",
                principalTable: "candidate_profiles",
                principalColumn: "candidate_id");

            migrationBuilder.AddForeignKey(
                name: "FK_payment_transactions_employer_profiles_EmployerProfileEmplo~",
                table: "payment_transactions",
                column: "EmployerProfileEmployerId",
                principalTable: "employer_profiles",
                principalColumn: "employer_id");
        }
    }
}