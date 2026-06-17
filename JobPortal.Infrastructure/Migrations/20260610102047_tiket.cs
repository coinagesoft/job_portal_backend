using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class tiket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_support_ticket_replies_support_tickets_SupportTicketTicketId",
                table: "support_ticket_replies");

            migrationBuilder.DropIndex(
                name: "IX_support_ticket_replies_SupportTicketTicketId",
                table: "support_ticket_replies");

            migrationBuilder.DropColumn(
                name: "SupportTicketTicketId",
                table: "support_ticket_replies");

            migrationBuilder.DropColumn(
                name: "is_admin_reply",
                table: "support_ticket_replies");

            migrationBuilder.RenameColumn(
                name: "replied_by",
                table: "support_ticket_replies",
                newName: "sender_id");

            migrationBuilder.AddColumn<string>(
                name: "sender_type",
                table: "support_ticket_replies",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sender_type",
                table: "support_ticket_replies");

            migrationBuilder.RenameColumn(
                name: "sender_id",
                table: "support_ticket_replies",
                newName: "replied_by");

            migrationBuilder.AddColumn<Guid>(
                name: "SupportTicketTicketId",
                table: "support_ticket_replies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_admin_reply",
                table: "support_ticket_replies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_support_ticket_replies_SupportTicketTicketId",
                table: "support_ticket_replies",
                column: "SupportTicketTicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_support_ticket_replies_support_tickets_SupportTicketTicketId",
                table: "support_ticket_replies",
                column: "SupportTicketTicketId",
                principalTable: "support_tickets",
                principalColumn: "TicketId");
        }
    }
}
