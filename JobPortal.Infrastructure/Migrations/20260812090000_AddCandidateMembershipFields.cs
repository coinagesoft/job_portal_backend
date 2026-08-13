using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    // Adds membership tracking to candidate_profiles so the platform can
    // tell which candidates actually paid the (admin-configured)
    // candidate lifetime membership fee, and which plan/when.
    //
    // NOTE: this migration was authored by hand (no `dotnet ef` tooling
    // available in the environment that produced it). It matches the
    // existing Postgres-style migrations in this project. Before/after
    // applying it, regenerate the EF model snapshot in your own dev
    // environment with:
    //     dotnet ef migrations add AddCandidateMembershipFields
    // if this file doesn't already match what `dotnet ef migrations add`
    // would generate for you (it should, since the entity + AppDbContext
    // changes are already included in this delivery).
    public partial class AddCandidateMembershipFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "ix_candidate_profiles_membership_plan_id",
                table: "candidate_profiles",
                column: "membership_plan_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_candidate_profiles_membership_plan_id",
                table: "candidate_profiles");

            migrationBuilder.DropColumn(
                name: "is_member",
                table: "candidate_profiles");

            migrationBuilder.DropColumn(
                name: "membership_plan_id",
                table: "candidate_profiles");

            migrationBuilder.DropColumn(
                name: "membership_purchased_at",
                table: "candidate_profiles");
        }
    }
}