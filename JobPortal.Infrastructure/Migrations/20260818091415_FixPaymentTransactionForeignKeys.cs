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

            migrationBuilder.AddColumn<bool>(
                name: "is_member",
                table: "candidate_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "membership_plan_id",
                table: "candidate_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "membership_purchased_at",
                table: "candidate_profiles",
                type: "timestamp with time zone",
                nullable: true);

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

            migrationBuilder.DropColumn(
                name: "is_member",
                table: "candidate_profiles");

            migrationBuilder.DropColumn(
                name: "membership_plan_id",
                table: "candidate_profiles");

            migrationBuilder.DropColumn(
                name: "membership_purchased_at",
                table: "candidate_profiles");

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
