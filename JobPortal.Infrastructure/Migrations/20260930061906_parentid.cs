using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class parentid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentSuggestionId",
                table: "homepage_suggestions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_homepage_suggestions_ParentSuggestionId",
                table: "homepage_suggestions",
                column: "ParentSuggestionId");

            migrationBuilder.AddForeignKey(
                name: "FK_homepage_suggestions_homepage_suggestions_ParentSuggestionId",
                table: "homepage_suggestions",
                column: "ParentSuggestionId",
                principalTable: "homepage_suggestions",
                principalColumn: "SuggestionId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_homepage_suggestions_homepage_suggestions_ParentSuggestionId",
                table: "homepage_suggestions");

            migrationBuilder.DropIndex(
                name: "IX_homepage_suggestions_ParentSuggestionId",
                table: "homepage_suggestions");

            migrationBuilder.DropColumn(
                name: "ParentSuggestionId",
                table: "homepage_suggestions");
        }
    }
}
