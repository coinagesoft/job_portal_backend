using System;
using Microsoft.EntityFrameworkCore.Migrations;


#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
  
    public partial class employer_sub_user1 : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
       
            migrationBuilder.CreateTable(
                name: "employer_sub_users",
                columns: table => new
                {
                    SubUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployerId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubUserName = table.Column<string>(type: "text", nullable: false),
                    SubUserEmail = table.Column<string>(type: "text", nullable: false),
                    SubUserMobile = table.Column<string>(type: "text", nullable: true),
                    SubUserCountryCode = table.Column<string>(type: "text", nullable: true),
                    SubUserRole = table.Column<string>(type: "text", nullable: false),
                    InviteToken = table.Column<Guid>(type: "uuid", nullable: true),
                    InviteExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InviteAccepted = table.Column<bool>(type: "boolean", nullable: false),
                    CanSearchCandidates = table.Column<bool>(type: "boolean", nullable: false),
                    CanUnlockProfiles = table.Column<bool>(type: "boolean", nullable: false),
                    CanPostJobs = table.Column<bool>(type: "boolean", nullable: false),
                    CanManageApplications = table.Column<bool>(type: "boolean", nullable: false),
                    SubUserStatus = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeactivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employer_sub_users", x => x.SubUserId);
                    table.ForeignKey(
                        name: "FK_employer_sub_users_employer_profiles_EmployerId",
                        column: x => x.EmployerId,
                        principalTable: "employer_profiles",
                        principalColumn: "employer_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employer_sub_users_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });


            migrationBuilder.CreateIndex(
                name: "IX_employer_sub_users_EmployerId",
                table: "employer_sub_users",
                column: "EmployerId");

            migrationBuilder.CreateIndex(
                name: "IX_employer_sub_users_UserId",
                table: "employer_sub_users",
                column: "UserId");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropTable(
                name: "employer_sub_users");

        }
    }
}
