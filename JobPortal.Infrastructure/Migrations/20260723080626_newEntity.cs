using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class newEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "EmployerVerificationDocuments");

            migrationBuilder.DropColumn(
                name: "DocumentName",
                table: "EmployerVerificationDocuments");

            migrationBuilder.AddColumn<Guid>(
                name: "DocumentTypeId",
                table: "EmployerVerificationDocuments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "BadgeType",
                table: "employer_badges",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "VerificationDocumentMasters",
                columns: table => new
                {
                    DocumentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    DocumentName = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    IsMandatory = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresVerification = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystemDocument = table.Column<bool>(type: "boolean", nullable: false),
                    AllowMultipleUploads = table.Column<bool>(type: "boolean", nullable: false),
                    AllowCustomDocument = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationDocumentMasters", x => x.DocumentTypeId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployerVerificationDocuments_DocumentTypeId",
                table: "EmployerVerificationDocuments",
                column: "DocumentTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployerVerificationDocuments_VerificationDocumentMasters_D~",
                table: "EmployerVerificationDocuments",
                column: "DocumentTypeId",
                principalTable: "VerificationDocumentMasters",
                principalColumn: "DocumentTypeId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployerVerificationDocuments_VerificationDocumentMasters_D~",
                table: "EmployerVerificationDocuments");

            migrationBuilder.DropTable(
                name: "VerificationDocumentMasters");

            migrationBuilder.DropIndex(
                name: "IX_EmployerVerificationDocuments_DocumentTypeId",
                table: "EmployerVerificationDocuments");

            migrationBuilder.DropColumn(
                name: "DocumentTypeId",
                table: "EmployerVerificationDocuments");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "EmployerVerificationDocuments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DocumentName",
                table: "EmployerVerificationDocuments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "BadgeType",
                table: "employer_badges",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
