using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    // Fixes: "Admin deletes a sub-user and then tries to add the same
    // user again. It shows 'Email already exists.'"
    //
    // Root cause: DeleteSubAdminAsync soft-deletes (users.IsDeleted =
    // true) rather than removing the row, but uq_users_email /
    // uq_users_mobile were plain, unconditional unique indexes. Postgres
    // enforced uniqueness across ALL rows, deleted or not, so re-adding a
    // sub-admin with the same email/mobile always failed at the database
    // level even after the app-level "already exists" check is fixed to
    // ignore deleted users.
    //
    // Fix: replace both indexes with partial unique indexes that only
    // apply to non-deleted rows (WHERE "IsDeleted" = false), so a
    // deleted user's email/mobile becomes free to reuse, while active
    // users' emails/mobiles stay unique as before.
    //
    // NOTE: this migration was authored by hand (no `dotnet ef` tooling
    // available in the environment that produced it), following the same
    // approach as AddCandidateLocationFields. Before/after applying it,
    // regenerate the EF model snapshot in your own dev environment with:
    //     dotnet ef migrations add AllowReuseOfDeletedUserEmailAndMobile
    // if this file doesn't already match what `dotnet ef migrations add`
    // would generate for you (it should, since the entity/config changes
    // are already reflected in the updated model snapshot in this delivery).
    public partial class AllowReuseOfDeletedUserEmailAndMobile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_users_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "uq_users_mobile",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "uq_users_email",
                table: "users",
                column: "email",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "uq_users_mobile",
                table: "users",
                column: "mobile_number",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_users_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "uq_users_mobile",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "uq_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_users_mobile",
                table: "users",
                column: "mobile_number",
                unique: true);
        }
    }
}