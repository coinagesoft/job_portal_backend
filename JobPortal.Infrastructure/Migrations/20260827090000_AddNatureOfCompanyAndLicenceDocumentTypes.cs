using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    // Client suggestion (registration page): ask "Nature of Company"
    // (Recruitment Agency | Employer) during Step 2 of recruiter
    // registration, and "Do you place candidates internationally?"
    // (asked for both natures). These two answers decide which licence
    // uploads are required on Step 4:
    //   - Certificate of Incorporation: every registrant (mandatory)
    //   - Recruitment License: Recruitment Agency only
    //   - POE License + RPSL License: whoever answers "Yes" to placing
    //     candidates internationally, regardless of nature
    //
    // This migration:
    //   1. Adds nature_of_company / places_candidates_internationally to
    //      registration_sessions (working copy, Step 2 -> Step 5) and
    //      employer_profiles (permanent copy, kept after submission so
    //      Admin can see it later).
    //   2. Seeds the four document types into VerificationDocumentMasters
    //      if they don't already exist (guarded — Admin may already have
    //      added some of these by hand via Admin > Document Types).
    //      Certificate of Incorporation is seeded as mandatory (applies to
    //      everyone); the other three are seeded as optional at the
    //      master-data level — which of them is actually *required* for a
    //      given registrant is decided client-side from the two new
    //      answers (see requiredDocumentNames in the registration form),
    //      not by a blanket per-document-type flag.
    //
    // NOTE: this migration was authored by hand (no `dotnet ef` tooling
    // available in the environment that produced it), following the same
    // approach as AddCandidateLocationFields / AllowReuseOfDeletedUserEmailAndMobile.
    // Before/after applying it, regenerate the EF model snapshot in your
    // own dev environment with:
    //     dotnet ef migrations add AddNatureOfCompanyAndLicenceDocumentTypes
    // if this file doesn't already match what `dotnet ef migrations add`
    // would generate for you (it should, since the entity + AppDbContext
    // changes are already included in this delivery).
    public partial class AddNatureOfCompanyAndLicenceDocumentTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NatureOfCompany",
                table: "registration_sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PlacesCandidatesInternationally",
                table: "registration_sessions",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nature_of_company",
                table: "employer_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "places_candidates_internationally",
                table: "employer_profiles",
                type: "boolean",
                nullable: true);

            // ── Seed the four document types, skipping any that Admin
            // has already created by hand (matched by DocumentName). ──
            migrationBuilder.Sql(@"
                INSERT INTO ""VerificationDocumentMasters""
                    (""DocumentTypeId"", ""Code"", ""DocumentName"", ""Category"",
                     ""IsMandatory"", ""IsActive"", ""RequiresVerification"",
                     ""IsSystemDocument"", ""AllowMultipleUploads"", ""AllowCustomDocument"",
                     ""DisplayOrder"", ""Description"", ""CreatedAt"", ""UpdatedAt"")
                SELECT gen_random_uuid(), 'CERT_INCORPORATION', 'Certificate of Incorporation',
                       'Company Registration', true, true, true, true, false, false,
                       100, 'Required for every company registering as a recruiter/employer.',
                       now(), now()
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""VerificationDocumentMasters""
                    WHERE ""DocumentName"" = 'Certificate of Incorporation'
                );

                INSERT INTO ""VerificationDocumentMasters""
                    (""DocumentTypeId"", ""Code"", ""DocumentName"", ""Category"",
                     ""IsMandatory"", ""IsActive"", ""RequiresVerification"",
                     ""IsSystemDocument"", ""AllowMultipleUploads"", ""AllowCustomDocument"",
                     ""DisplayOrder"", ""Description"", ""CreatedAt"", ""UpdatedAt"")
                SELECT gen_random_uuid(), 'RECRUITMENT_LICENSE', 'Recruitment License',
                       'Licence', false, true, true, true, false, false,
                       101, 'Required when Nature of Company is Recruitment Agency.',
                       now(), now()
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""VerificationDocumentMasters""
                    WHERE ""DocumentName"" = 'Recruitment License'
                );

                INSERT INTO ""VerificationDocumentMasters""
                    (""DocumentTypeId"", ""Code"", ""DocumentName"", ""Category"",
                     ""IsMandatory"", ""IsActive"", ""RequiresVerification"",
                     ""IsSystemDocument"", ""AllowMultipleUploads"", ""AllowCustomDocument"",
                     ""DisplayOrder"", ""Description"", ""CreatedAt"", ""UpdatedAt"")
                SELECT gen_random_uuid(), 'POE_LICENSE', 'POE License',
                       'Licence', false, true, true, true, false, false,
                       102, 'Required when the registrant places candidates internationally.',
                       now(), now()
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""VerificationDocumentMasters""
                    WHERE ""DocumentName"" = 'POE License'
                );

                INSERT INTO ""VerificationDocumentMasters""
                    (""DocumentTypeId"", ""Code"", ""DocumentName"", ""Category"",
                     ""IsMandatory"", ""IsActive"", ""RequiresVerification"",
                     ""IsSystemDocument"", ""AllowMultipleUploads"", ""AllowCustomDocument"",
                     ""DisplayOrder"", ""Description"", ""CreatedAt"", ""UpdatedAt"")
                SELECT gen_random_uuid(), 'RPSL_LICENSE', 'RPSL License',
                       'Licence', false, true, true, true, false, false,
                       103, 'Required when the registrant places candidates internationally.',
                       now(), now()
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""VerificationDocumentMasters""
                    WHERE ""DocumentName"" = 'RPSL License'
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only remove the rows this migration itself would have
            // inserted — matched by the codes above, so we don't delete
            // anything Admin added independently with the same names.
            migrationBuilder.Sql(@"
                DELETE FROM ""VerificationDocumentMasters""
                WHERE ""Code"" IN ('CERT_INCORPORATION', 'RECRUITMENT_LICENSE', 'POE_LICENSE', 'RPSL_LICENSE');
            ");

            migrationBuilder.DropColumn(
                name: "NatureOfCompany",
                table: "registration_sessions");

            migrationBuilder.DropColumn(
                name: "PlacesCandidatesInternationally",
                table: "registration_sessions");

            migrationBuilder.DropColumn(
                name: "nature_of_company",
                table: "employer_profiles");

            migrationBuilder.DropColumn(
                name: "places_candidates_internationally",
                table: "employer_profiles");
        }
    }
}