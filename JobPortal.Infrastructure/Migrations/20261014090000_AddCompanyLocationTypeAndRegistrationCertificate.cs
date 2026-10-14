using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using JobPortal.Infrastructure.Persistence;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <summary>
    /// Recruiter licence rule by company location:
    ///   Onshore + outside India -> POE License (not RPSL)
    ///   Offshore                -> RPSL License OR Registration Certificate (no POE)
    ///
    /// 1. Persists the company location type on the permanent employer profile
    ///    (previously it only lived on registration_sessions and was lost after
    ///    registration, so Admin could not see it).
    /// 2. Seeds the "Registration Certificate" document type, which the offshore
    ///    alternative needs but which was never created.
    /// 3. Updates the POE / RPSL descriptions to match the new rule.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261014090000_AddCompanyLocationTypeAndRegistrationCertificate")]
    public partial class AddCompanyLocationTypeAndRegistrationCertificate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "company_location_type",
                table: "employer_profiles",
                type: "text",
                nullable: true);

            // Seed "Registration Certificate" (optional, active) unless Admin
            // already created a type with that name.
            migrationBuilder.Sql(@"
                INSERT INTO ""VerificationDocumentMasters""
                    (""DocumentTypeId"", ""Code"", ""DocumentName"", ""Category"",
                     ""IsMandatory"", ""IsActive"", ""RequiresVerification"",
                     ""IsSystemDocument"", ""AllowMultipleUploads"", ""AllowCustomDocument"",
                     ""DisplayOrder"", ""Description"", ""CreatedAt"", ""UpdatedAt"")
                SELECT gen_random_uuid(), 'REGISTRATION_CERTIFICATE', 'Registration Certificate',
                       'Licence', false, true, true, true, false, false,
                       104, 'Offshore companies: upload this or an RPSL License.',
                       now(), now()
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""VerificationDocumentMasters""
                    WHERE ""DocumentName"" = 'Registration Certificate'
                );

                UPDATE ""VerificationDocumentMasters""
                SET ""Description"" = 'Required for Onshore companies outside India. Not required for Offshore companies.',
                    ""UpdatedAt"" = now()
                WHERE ""Code"" = 'POE_LICENSE';

                UPDATE ""VerificationDocumentMasters""
                SET ""Description"" = 'Offshore companies: upload this or a Registration Certificate. Not applicable to Onshore companies outside India.',
                    ""UpdatedAt"" = now()
                WHERE ""Code"" = 'RPSL_LICENSE';
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""VerificationDocumentMasters""
                WHERE ""Code"" = 'REGISTRATION_CERTIFICATE';
            ");

            migrationBuilder.DropColumn(
                name: "company_location_type",
                table: "employer_profiles");
        }
    }
}