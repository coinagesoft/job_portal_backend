using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class emprof : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RpslLicenceS3Url",
                table: "registration_sessions",
                newName: "RpslLicenceUrl");

            migrationBuilder.RenameColumn(
                name: "PoeLicenceS3Url",
                table: "registration_sessions",
                newName: "RpslLicencePublicId");

            migrationBuilder.AddColumn<string>(
                name: "CompanyLogoPublicId",
                table: "registration_sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PoeLicencePublicId",
                table: "registration_sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PoeLicenceUrl",
                table: "registration_sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyLogoPublicId",
                table: "employer_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PoeLicencePublicId",
                table: "employer_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RpslLicencePublicId",
                table: "employer_profiles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyLogoPublicId",
                table: "registration_sessions");

            migrationBuilder.DropColumn(
                name: "PoeLicencePublicId",
                table: "registration_sessions");

            migrationBuilder.DropColumn(
                name: "PoeLicenceUrl",
                table: "registration_sessions");

            migrationBuilder.DropColumn(
                name: "CompanyLogoPublicId",
                table: "employer_profiles");

            migrationBuilder.DropColumn(
                name: "PoeLicencePublicId",
                table: "employer_profiles");

            migrationBuilder.DropColumn(
                name: "RpslLicencePublicId",
                table: "employer_profiles");

            migrationBuilder.RenameColumn(
                name: "RpslLicenceUrl",
                table: "registration_sessions",
                newName: "RpslLicenceS3Url");

            migrationBuilder.RenameColumn(
                name: "RpslLicencePublicId",
                table: "registration_sessions",
                newName: "PoeLicenceS3Url");
        }
    }
}
