using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class recruitersessiondocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegistrationSessionDocuments",
                columns: table => new
                {
                    RegistrationDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomDocumentName = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: true),
                    DetectedDocumentType = table.Column<string>(type: "text", nullable: true),
                    DocumentNumber = table.Column<string>(type: "text", nullable: true),
                    IssuingAuthority = table.Column<string>(type: "text", nullable: true),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ParsedDataJson = table.Column<string>(type: "text", nullable: true),
                    AiConfidenceScore = table.Column<decimal>(type: "numeric", nullable: true),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    FileUrl = table.Column<string>(type: "text", nullable: false),
                    PublicId = table.Column<string>(type: "text", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationSessionDocuments", x => x.RegistrationDocumentId);
                    table.ForeignKey(
                        name: "FK_RegistrationSessionDocuments_VerificationDocumentMasters_Do~",
                        column: x => x.DocumentTypeId,
                        principalTable: "VerificationDocumentMasters",
                        principalColumn: "DocumentTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegistrationSessionDocuments_registration_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "registration_sessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationSessionDocuments_DocumentTypeId",
                table: "RegistrationSessionDocuments",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationSessionDocuments_SessionId",
                table: "RegistrationSessionDocuments",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrationSessionDocuments");
        }
    }
}
