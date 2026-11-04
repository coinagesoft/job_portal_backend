using JobPortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <summary>
    /// Data-only repair. The candidate-facing company page matches licence
    /// documents on VerificationDocumentMasters.Code (POE_LICENSE,
    /// RPSL_LICENSE, REGISTRATION_CERTIFICATE). Document types created by
    /// hand in Admin got a random code instead, and REGISTRATION_CERTIFICATE
    /// was never seeded at all.
    ///
    ///   1. Re-code existing masters whose NAME is a licence document but
    ///      whose Code is wrong (only when the canonical code is still free).
    ///   2. Seed Registration Certificate if no such master exists.
    ///
    /// No schema change, so the model snapshot is unaffected.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261104100000_NormalizeLicenceDocumentCodes")]
    public partial class NormalizeLicenceDocumentCodes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""VerificationDocumentMasters"" m
                SET ""Code"" = 'POE_LICENSE', ""UpdatedAt"" = now()
                WHERE lower(trim(m.""DocumentName"")) IN ('poe license', 'poe licence')
                  AND m.""Code"" <> 'POE_LICENSE'
                  AND NOT EXISTS (SELECT 1 FROM ""VerificationDocumentMasters"" x WHERE x.""Code"" = 'POE_LICENSE');

                UPDATE ""VerificationDocumentMasters"" m
                SET ""Code"" = 'RPSL_LICENSE', ""UpdatedAt"" = now()
                WHERE lower(trim(m.""DocumentName"")) IN ('rpsl license', 'rpsl licence')
                  AND m.""Code"" <> 'RPSL_LICENSE'
                  AND NOT EXISTS (SELECT 1 FROM ""VerificationDocumentMasters"" x WHERE x.""Code"" = 'RPSL_LICENSE');

                UPDATE ""VerificationDocumentMasters"" m
                SET ""Code"" = 'REGISTRATION_CERTIFICATE', ""UpdatedAt"" = now()
                WHERE lower(trim(m.""DocumentName"")) IN ('registration certificate', 'company registration certificate')
                  AND m.""Code"" <> 'REGISTRATION_CERTIFICATE'
                  AND NOT EXISTS (SELECT 1 FROM ""VerificationDocumentMasters"" x WHERE x.""Code"" = 'REGISTRATION_CERTIFICATE');

                INSERT INTO ""VerificationDocumentMasters""
                    (""DocumentTypeId"", ""Code"", ""DocumentName"", ""Category"",
                     ""IsMandatory"", ""IsActive"", ""RequiresVerification"",
                     ""IsSystemDocument"", ""AllowMultipleUploads"", ""AllowCustomDocument"",
                     ""DisplayOrder"", ""Description"", ""CreatedAt"", ""UpdatedAt"")
                SELECT gen_random_uuid(), 'REGISTRATION_CERTIFICATE', 'Registration Certificate',
                       'Licence', false, true, true, true, false, false,
                       (SELECT COALESCE(MAX(""DisplayOrder""), 0) + 1 FROM ""VerificationDocumentMasters""),
                       'Required for Offshore companies that do not hold an RPSL Licence.',
                       now(), now()
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""VerificationDocumentMasters""
                    WHERE ""Code"" = 'REGISTRATION_CERTIFICATE'
                       OR lower(trim(""DocumentName"")) IN ('registration certificate', 'company registration certificate')
                );
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: the previous (random / missing) codes
            // were a defect, so there is nothing worth restoring.
        }
    }
}