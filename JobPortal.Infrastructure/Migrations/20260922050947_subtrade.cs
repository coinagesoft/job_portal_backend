using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class subtrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HomepageSubTrades",
                columns: table => new
                {
                    SubTradeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TradeCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomepageSubTrades", x => x.SubTradeId);
                    table.ForeignKey(
                        name: "FK_HomepageSubTrades_homepage_trade_categories_TradeCategoryId",
                        column: x => x.TradeCategoryId,
                        principalTable: "homepage_trade_categories",
                        principalColumn: "TradeCategoryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HomepageSubTrades_TradeCategoryId_Name",
                table: "HomepageSubTrades",
                columns: new[] { "TradeCategoryId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomepageSubTrades");
        }
    }
}
