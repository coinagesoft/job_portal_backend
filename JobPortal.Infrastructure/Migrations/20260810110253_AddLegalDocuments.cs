using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLegalDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "legal_documents",
                columns: table => new
                {
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DraftContent = table.Column<string>(type: "text", nullable: false),
                    DraftEffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishedContent = table.Column<string>(type: "text", nullable: true),
                    PublishedEffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_legal_documents", x => x.DocumentId);
                    table.ForeignKey(
                        name: "FK_legal_documents_AdminUsers_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "AdminUsers",
                        principalColumn: "AdminId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "legal_documents",
                columns: new[] { "DocumentId", "DraftContent", "DraftEffectiveDate", "PublishedAt", "PublishedContent", "PublishedEffectiveDate", "Status", "Title", "Type", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("6f2a6c2e-6a3b-4a0a-8a2e-0a6f6a2c2a01"), "<h2>Privacy Policy</h2><p>Write or paste your privacy policy here.</p>", null, null, null, null, "Draft", "Privacy Policy", "privacy", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("6f2a6c2e-6a3b-4a0a-8a2e-0a6f6a2c2a02"), "<h2>Terms &amp; Conditions</h2><p>Write or paste your terms of service here.</p>", null, null, null, null, "Draft", "Terms & Conditions", "terms", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_legal_documents_Type",
                table: "legal_documents",
                column: "Type",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_legal_documents_UpdatedBy",
                table: "legal_documents",
                column: "UpdatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "legal_documents");
        }
    }
}
