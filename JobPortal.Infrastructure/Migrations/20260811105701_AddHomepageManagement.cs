using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHomepageManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "homepage_departments",
                columns: table => new
                {
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_homepage_departments", x => x.DepartmentId);
                });

            migrationBuilder.CreateTable(
                name: "homepage_hero",
                columns: table => new
                {
                    HeroId = table.Column<Guid>(type: "uuid", nullable: false),
                    Headline = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Subheadline = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    SearchPlaceholder = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CtaText = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CtaLink = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    BannerImageUrl = table.Column<string>(type: "text", nullable: true),
                    BannerImagePublicId = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_homepage_hero", x => x.HeroId);
                    table.ForeignKey(
                        name: "FK_homepage_hero_AdminUsers_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "AdminUsers",
                        principalColumn: "AdminId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "homepage_industries",
                columns: table => new
                {
                    IndustryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: true),
                    IconUrl = table.Column<string>(type: "text", nullable: true),
                    JobCountOverride = table.Column<int>(type: "integer", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ShowInDropdown = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_homepage_industries", x => x.IndustryId);
                });

            migrationBuilder.CreateTable(
                name: "homepage_locations",
                columns: table => new
                {
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Country = table.Column<string>(type: "text", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    ImagePublicId = table.Column<string>(type: "text", nullable: true),
                    JobCountOverride = table.Column<int>(type: "integer", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_homepage_locations", x => x.LocationId);
                });

            migrationBuilder.CreateTable(
                name: "homepage_registration_industries",
                columns: table => new
                {
                    RegistrationIndustryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_homepage_registration_industries", x => x.RegistrationIndustryId);
                });

            migrationBuilder.CreateTable(
                name: "homepage_roles",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    IconUrl = table.Column<string>(type: "text", nullable: true),
                    JobCountOverride = table.Column<int>(type: "integer", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_homepage_roles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "homepage_statistics",
                columns: table => new
                {
                    StatisticsId = table.Column<Guid>(type: "uuid", nullable: false),
                    Items = table.Column<string>(type: "jsonb", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_homepage_statistics", x => x.StatisticsId);
                });

            migrationBuilder.CreateTable(
                name: "homepage_suggestions",
                columns: table => new
                {
                    SuggestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SuggestedName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    SubmittedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubmittedByName = table.Column<string>(type: "text", nullable: true),
                    SubmittedByEmail = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AdminNote = table.Column<string>(type: "text", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_homepage_suggestions", x => x.SuggestionId);
                    table.ForeignKey(
                        name: "FK_homepage_suggestions_AdminUsers_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "AdminUsers",
                        principalColumn: "AdminId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_homepage_suggestions_users_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "homepage_trade_categories",
                columns: table => new
                {
                    TradeCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_homepage_trade_categories", x => x.TradeCategoryId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_homepage_departments_DisplayOrder",
                table: "homepage_departments",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_homepage_hero_UpdatedBy",
                table: "homepage_hero",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_homepage_industries_DisplayOrder",
                table: "homepage_industries",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_homepage_locations_DisplayOrder",
                table: "homepage_locations",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_homepage_registration_industries_DisplayOrder",
                table: "homepage_registration_industries",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_homepage_roles_DisplayOrder",
                table: "homepage_roles",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_homepage_suggestions_ReviewedBy",
                table: "homepage_suggestions",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_homepage_suggestions_Status",
                table: "homepage_suggestions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_homepage_suggestions_SubmittedByUserId",
                table: "homepage_suggestions",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_homepage_trade_categories_DisplayOrder",
                table: "homepage_trade_categories",
                column: "DisplayOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "homepage_departments");

            migrationBuilder.DropTable(
                name: "homepage_hero");

            migrationBuilder.DropTable(
                name: "homepage_industries");

            migrationBuilder.DropTable(
                name: "homepage_locations");

            migrationBuilder.DropTable(
                name: "homepage_registration_industries");

            migrationBuilder.DropTable(
                name: "homepage_roles");

            migrationBuilder.DropTable(
                name: "homepage_statistics");

            migrationBuilder.DropTable(
                name: "homepage_suggestions");

            migrationBuilder.DropTable(
                name: "homepage_trade_categories");
        }
    }
}
