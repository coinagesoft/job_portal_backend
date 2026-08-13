using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class requestEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RequestId",
                table: "EmployerVerificationDocuments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomDocumentName",
                table: "EmployerDocumentRequests",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployerVerificationDocuments_RequestId",
                table: "EmployerVerificationDocuments",
                column: "RequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployerVerificationDocuments_EmployerDocumentRequests_Requ~",
                table: "EmployerVerificationDocuments",
                column: "RequestId",
                principalTable: "EmployerDocumentRequests",
                principalColumn: "RequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployerVerificationDocuments_EmployerDocumentRequests_Requ~",
                table: "EmployerVerificationDocuments");

            migrationBuilder.DropIndex(
                name: "IX_EmployerVerificationDocuments_RequestId",
                table: "EmployerVerificationDocuments");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "EmployerVerificationDocuments");

            migrationBuilder.DropColumn(
                name: "CustomDocumentName",
                table: "EmployerDocumentRequests");
        }
    }
}
