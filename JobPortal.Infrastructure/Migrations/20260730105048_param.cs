using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class param : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployerVerificationDocuments_VerificationDocumentMasters_D~",
                table: "EmployerVerificationDocuments");

            migrationBuilder.AlterColumn<Guid>(
                name: "DocumentTypeId",
                table: "EmployerVerificationDocuments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "EmployerVerificationDocuments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomDocumentName",
                table: "EmployerVerificationDocuments",
                type: "text",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployerVerificationDocuments_VerificationDocumentMasters_D~",
                table: "EmployerVerificationDocuments",
                column: "DocumentTypeId",
                principalTable: "VerificationDocumentMasters",
                principalColumn: "DocumentTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployerVerificationDocuments_VerificationDocumentMasters_D~",
                table: "EmployerVerificationDocuments");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "EmployerVerificationDocuments");

            migrationBuilder.DropColumn(
                name: "CustomDocumentName",
                table: "EmployerVerificationDocuments");

            migrationBuilder.AlterColumn<Guid>(
                name: "DocumentTypeId",
                table: "EmployerVerificationDocuments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployerVerificationDocuments_VerificationDocumentMasters_D~",
                table: "EmployerVerificationDocuments",
                column: "DocumentTypeId",
                principalTable: "VerificationDocumentMasters",
                principalColumn: "DocumentTypeId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
