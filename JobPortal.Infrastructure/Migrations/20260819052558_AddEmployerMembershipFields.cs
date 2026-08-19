using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    // NOTE: this migration originally also re-dropped/re-added FKs, indexes,
    // and column alterations on EmployerDocumentRequests and payment_transactions
    // (RequestedByAdminAdminId, CandidateProfileCandidateId/EmployerProfileEmployerId,
    // Status/Message/CustomDocumentName/DocumentTypeId column types, etc). Those
    // were scaffolded against a stale model snapshot — that work was already
    // done by migrations "fk" (20260818052544), "fk1" (20260818072423), and
    // "FixPaymentTransactionForeignKeys" (20260818091415), so re-running it here
    // fails (DROP CONSTRAINT ... does not exist / duplicate FK name, etc).
    // Trimmed down to the one thing this migration actually needs to do:
    // add the recruiter (employer) lifetime-membership columns to
    // employer_profiles, mirroring CandidateProfile.IsMember/MembershipPlanId/
    // MembershipPurchasedAt (see EmployerProfile.cs).
    public partial class AddEmployerMembershipFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}