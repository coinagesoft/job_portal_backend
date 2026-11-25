using JobPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <summary>
    /// Data-only. The company-profile page uploads GST, PAN and Business
    /// Registration against VerificationDocumentMasters.DocumentTypeId (a Guid),
    /// but these three masters were never seeded, so uploads failed with
    /// "The value 'GST' is not valid for DocumentTypeId". Seeds them (skipping
    /// any Admin already created by code or name). No schema change.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261124100000_SeedGstPanBusinessRegistrationDocumentTypes")]
    public partial class SeedGstPanBusinessRegistrationDocumentTypes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO ""VerificationDocumentMasters""
                    (""DocumentTypeId"", ""Code"", ""DocumentName"", ""Category"",
                     ""IsMandatory"", ""IsActive"", ""RequiresVerification"",
                     ""IsSystemDocument"", ""AllowMultipleUploads"", ""AllowCustomDocument"",
                     ""DisplayOrder"", ""Description"", ""CreatedAt"", ""UpdatedAt"")
                SELECT gen_random_uuid(), 'GST', 'GST Certificate',
                       'Company Registration', true, true, true, true, false, false,
                       1, 'Required company verification document.',
                       now(), now()
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""VerificationDocumentMasters""
                    WHERE ""Code"" = 'GST'
                       OR lower(trim(""DocumentName"")) = lower('GST Certificate')
                );

                INSERT INTO ""VerificationDocumentMasters""
                    (""DocumentTypeId"", ""Code"", ""DocumentName"", ""Category"",
                     ""IsMandatory"", ""IsActive"", ""RequiresVerification"",
                     ""IsSystemDocument"", ""AllowMultipleUploads"", ""AllowCustomDocument"",
                     ""DisplayOrder"", ""Description"", ""CreatedAt"", ""UpdatedAt"")
                SELECT gen_random_uuid(), 'PAN', 'PAN Card',
                       'Company Registration', true, true, true, true, false, false,
                       2, 'Required company verification document.',
                       now(), now()
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""VerificationDocumentMasters""
                    WHERE ""Code"" = 'PAN'
                       OR lower(trim(""DocumentName"")) = lower('PAN Card')
                );

                INSERT INTO ""VerificationDocumentMasters""
                    (""DocumentTypeId"", ""Code"", ""DocumentName"", ""Category"",
                     ""IsMandatory"", ""IsActive"", ""RequiresVerification"",
                     ""IsSystemDocument"", ""AllowMultipleUploads"", ""AllowCustomDocument"",
                     ""DisplayOrder"", ""Description"", ""CreatedAt"", ""UpdatedAt"")
                SELECT gen_random_uuid(), 'BUSINESS_REGISTRATION', 'Business Registration',
                       'Company Registration', true, true, true, true, false, false,
                       3, 'Required company verification document.',
                       now(), now()
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""VerificationDocumentMasters""
                    WHERE ""Code"" = 'BUSINESS_REGISTRATION'
                       OR lower(trim(""DocumentName"")) = lower('Business Registration')
                );

                UPDATE ""VerificationDocumentMasters""
                SET ""IsMandatory"" = true, ""IsActive"" = true, ""UpdatedAt"" = now()
                WHERE ""Code"" IN ('GST', 'PAN', 'BUSINESS_REGISTRATION');
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
