using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class registrationsessionnewcol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           

          

            migrationBuilder.AddColumn<string>(
                name: "Gstn",
                table: "registration_sessions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateOnly>(
                name: "GstnRegistrationDate",
                table: "registration_sessions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pan",
                table: "registration_sessions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

         
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
      

            migrationBuilder.DropColumn(
                name: "Gstn",
                table: "registration_sessions");

            migrationBuilder.DropColumn(
                name: "GstnRegistrationDate",
                table: "registration_sessions");

            migrationBuilder.DropColumn(
                name: "Pan",
                table: "registration_sessions");
         
        }
    }
}
