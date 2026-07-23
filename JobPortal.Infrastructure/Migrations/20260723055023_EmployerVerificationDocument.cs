using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    public partial class EmployerVerificationDocument : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "VerificationDocumentId",
                table: "employer_badges",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmployerVerificationDocuments",
                columns: table => new
                {
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),

                    EmployerId = table.Column<Guid>(type: "uuid", nullable: false),

                    DocumentName = table.Column<string>(type: "text", nullable: false),

                    Category = table.Column<string>(type: "text", nullable: false),

                    DocumentNumber = table.Column<string>(type: "text", nullable: true),

                    IssuingAuthority = table.Column<string>(type: "text", nullable: true),

                    IssueDate = table.Column<DateOnly>(type: "date", nullable: true),

                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),

                    FileName = table.Column<string>(type: "text", nullable: false),

                    FileUrl = table.Column<string>(type: "text", nullable: false),

                    PublicId = table.Column<string>(type: "text", nullable: false),

                    Status = table.Column<int>(type: "integer", nullable: false),

                    VerifiedBy = table.Column<Guid>(type: "uuid", nullable: true),

                    UploadedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false),

                    VerifiedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true),

                    Remarks = table.Column<string>(
                        type: "text",
                        nullable: true),

                    IsDeleted = table.Column<bool>(
                        type: "boolean",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_EmployerVerificationDocuments",
                        x => x.DocumentId);

                    table.ForeignKey(
                        name: "FK_EmployerVerificationDocuments_employer_profiles_EmployerId",
                        column: x => x.EmployerId,
                        principalTable: "employer_profiles",
                        principalColumn: "employer_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_employer_badges_VerificationDocumentId",
                table: "employer_badges",
                column: "VerificationDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployerVerificationDocuments_EmployerId",
                table: "EmployerVerificationDocuments",
                column: "EmployerId");

            migrationBuilder.AddForeignKey(
                name: "FK_employer_badges_EmployerVerificationDocuments_VerificationDocumentId",
                table: "employer_badges",
                column: "VerificationDocumentId",
                principalTable: "EmployerVerificationDocuments",
                principalColumn: "DocumentId",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_employer_badges_EmployerVerificationDocuments_VerificationDocumentId",
                table: "employer_badges");

            migrationBuilder.DropTable(
                name: "EmployerVerificationDocuments");

            migrationBuilder.DropIndex(
                name: "IX_employer_badges_VerificationDocumentId",
                table: "employer_badges");

            migrationBuilder.DropColumn(
                name: "VerificationDocumentId",
                table: "employer_badges");
        }
    }
}