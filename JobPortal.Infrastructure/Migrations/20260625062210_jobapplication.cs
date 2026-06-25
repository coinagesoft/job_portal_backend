using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class jobapplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "motivation_message",
                table: "job_applications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "screening_answers",
                table: "job_applications",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "motivation_message",
                table: "job_applications");

            migrationBuilder.DropColumn(
                name: "screening_answers",
                table: "job_applications");
        }
    }
}
