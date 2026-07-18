using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditUsageTransactionActionBySnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Snapshots who performed each credit-spending action (owner or
            // sub-user) at the time it happened, so the transaction history
            // stays readable even after a sub-user is later deleted — a
            // live join against EmployerSubUsers can no longer resolve a
            // name at that point.
            migrationBuilder.AddColumn<string>(
                name: "ActionByName",
                table: "CreditUsageTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActionByRole",
                table: "CreditUsageTransactions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActionByName",
                table: "CreditUsageTransactions");

            migrationBuilder.DropColumn(
                name: "ActionByRole",
                table: "CreditUsageTransactions");
        }
    }
}