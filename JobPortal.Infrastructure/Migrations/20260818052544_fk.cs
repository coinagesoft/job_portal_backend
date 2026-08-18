using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class fk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployerDocumentRequests_AdminUsers_RequestedByAdminAdminId",
                table: "EmployerDocumentRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployerDocumentRequests_VerificationDocumentMasters_Docume~",
                table: "EmployerDocumentRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployerDocumentRequests_employer_profiles_EmployerId",
                table: "EmployerDocumentRequests");

            migrationBuilder.DropIndex(
                name: "IX_EmployerDocumentRequests_RequestedByAdminAdminId",
                table: "EmployerDocumentRequests");

            migrationBuilder.DropColumn(
                name: "RequestedByAdminAdminId",
                table: "EmployerDocumentRequests");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "EmployerDocumentRequests",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "EmployerDocumentRequests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomDocumentName",
                table: "EmployerDocumentRequests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

          

            migrationBuilder.CreateIndex(
                name: "IX_EmployerDocumentRequests_RequestedBy",
                table: "EmployerDocumentRequests",
                column: "RequestedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployerDocumentRequests_AdminUsers_RequestedBy",
                table: "EmployerDocumentRequests",
                column: "RequestedBy",
                principalTable: "AdminUsers",
                principalColumn: "AdminId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployerDocumentRequests_VerificationDocumentMasters_Docume~",
                table: "EmployerDocumentRequests",
                column: "DocumentTypeId",
                principalTable: "VerificationDocumentMasters",
                principalColumn: "DocumentTypeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployerDocumentRequests_employer_profiles_EmployerId",
                table: "EmployerDocumentRequests",
                column: "EmployerId",
                principalTable: "employer_profiles",
                principalColumn: "employer_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployerDocumentRequests_AdminUsers_RequestedBy",
                table: "EmployerDocumentRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployerDocumentRequests_VerificationDocumentMasters_Docume~",
                table: "EmployerDocumentRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployerDocumentRequests_employer_profiles_EmployerId",
                table: "EmployerDocumentRequests");

            migrationBuilder.DropIndex(
                name: "IX_EmployerDocumentRequests_RequestedBy",
                table: "EmployerDocumentRequests");


            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "EmployerDocumentRequests",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "EmployerDocumentRequests",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomDocumentName",
                table: "EmployerDocumentRequests",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestedByAdminAdminId",
                table: "EmployerDocumentRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_EmployerDocumentRequests_RequestedByAdminAdminId",
                table: "EmployerDocumentRequests",
                column: "RequestedByAdminAdminId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployerDocumentRequests_AdminUsers_RequestedByAdminAdminId",
                table: "EmployerDocumentRequests",
                column: "RequestedByAdminAdminId",
                principalTable: "AdminUsers",
                principalColumn: "AdminId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployerDocumentRequests_VerificationDocumentMasters_Docume~",
                table: "EmployerDocumentRequests",
                column: "DocumentTypeId",
                principalTable: "VerificationDocumentMasters",
                principalColumn: "DocumentTypeId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployerDocumentRequests_employer_profiles_EmployerId",
                table: "EmployerDocumentRequests",
                column: "EmployerId",
                principalTable: "employer_profiles",
                principalColumn: "employer_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
