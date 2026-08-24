using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    // Adds live-location tracking to candidate_profiles: last-known
    // lat/lng, whether the candidate has granted geolocation permission,
    // and when the location was last synced. Populated/updated by the
    // new GET/PUT /api/candidate/profile/location endpoint from the
    // web/mobile client once the candidate grants location permission.
    //
    // NOTE: this migration was authored by hand (no `dotnet ef` tooling
    // available in the environment that produced it), following the same
    // approach as AddCandidateMembershipFields. Before/after applying it,
    // regenerate the EF model snapshot in your own dev environment with:
    //     dotnet ef migrations add AddCandidateLocationFields
    // if this file doesn't already match what `dotnet ef migrations add`
    // would generate for you (it should, since the entity + AppDbContext
    // changes are already included in this delivery).
    public partial class AddCandidateLocationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "current_latitude",
                table: "candidate_profiles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "current_longitude",
                table: "candidate_profiles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "location_permission_granted",
                table: "candidate_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "location_updated_at",
                table: "candidate_profiles",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "current_latitude",
                table: "candidate_profiles");

            migrationBuilder.DropColumn(
                name: "current_longitude",
                table: "candidate_profiles");

            migrationBuilder.DropColumn(
                name: "location_permission_granted",
                table: "candidate_profiles");

            migrationBuilder.DropColumn(
                name: "location_updated_at",
                table: "candidate_profiles");
        }
    }
}