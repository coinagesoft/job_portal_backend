using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditAllocationHistorySubUserName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Snapshots the sub-user's display name at the time of the
            // allocation/reclaim event, so history entries stay readable
            // even after the sub-user is later deleted (their
            // EmployerSubUsers row is gone by then, so a live name lookup
            // would otherwise fall back to a bare GUID).
            migrationBuilder.AddColumn<string>(
                name: "SubUserName",
                table: "CreditAllocationHistory",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubUserName",
                table: "CreditAllocationHistory");
        }
    }
}