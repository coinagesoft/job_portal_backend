using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class employerdocumentrequest1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployerDocumentRequests",
                columns: table => new
                {
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployerId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequestedByAdminAdminId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployerDocumentRequests", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_EmployerDocumentRequests_AdminUsers_RequestedByAdminAdminId",
                        column: x => x.RequestedByAdminAdminId,
                        principalTable: "AdminUsers",
                        principalColumn: "AdminId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployerDocumentRequests_VerificationDocumentMasters_Docume~",
                        column: x => x.DocumentTypeId,
                        principalTable: "VerificationDocumentMasters",
                        principalColumn: "DocumentTypeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployerDocumentRequests_employer_profiles_EmployerId",
                        column: x => x.EmployerId,
                        principalTable: "employer_profiles",
                        principalColumn: "employer_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployerDocumentRequests_DocumentTypeId",
                table: "EmployerDocumentRequests",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployerDocumentRequests_EmployerId",
                table: "EmployerDocumentRequests",
                column: "EmployerId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployerDocumentRequests_RequestedByAdminAdminId",
                table: "EmployerDocumentRequests",
                column: "RequestedByAdminAdminId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployerDocumentRequests");
        }
    }
}
