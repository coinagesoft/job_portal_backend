using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class passportverification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "passport_verifications");

            migrationBuilder.CreateTable(
                name: "passport_verifications",
                columns: table => new
                {
                    verification_id = table.Column<Guid>(nullable: false),
                    candidate_id = table.Column<Guid>(nullable: false),
                    front_image_url = table.Column<string>(nullable: false),
                    back_image_url = table.Column<string>(nullable: true),
                    ai_extracted_name = table.Column<string>(nullable: true),
                    ai_extracted_dob = table.Column<DateOnly>(nullable: true),
                    ai_confidence_score = table.Column<decimal>(nullable: true),
                    admin_decision = table.Column<string>(nullable: false),
                    rejection_reason = table.Column<string>(nullable: true),
                    reviewed_by = table.Column<Guid>(nullable: true),
                    reviewed_at = table.Column<DateTime>(nullable: true),
                    created_at = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_passport_verifications", x => x.verification_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
