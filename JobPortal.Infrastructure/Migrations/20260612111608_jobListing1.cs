using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class jobListing1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CandidateCvDownload_candidate_cv_CandidateCvCvId",
                table: "CandidateCvDownload");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CandidateCvDownload",
                table: "CandidateCvDownload");

            migrationBuilder.RenameTable(
                name: "CandidateCvDownload",
                newName: "CandidateCvDownloads");

            migrationBuilder.RenameIndex(
                name: "IX_CandidateCvDownload_CandidateCvCvId",
                table: "CandidateCvDownloads",
                newName: "IX_CandidateCvDownloads_CandidateCvCvId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CandidateCvDownloads",
                table: "CandidateCvDownloads",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateCvDownloads_candidate_cv_CandidateCvCvId",
                table: "CandidateCvDownloads",
                column: "CandidateCvCvId",
                principalTable: "candidate_cv",
                principalColumn: "CvId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CandidateCvDownloads_candidate_cv_CandidateCvCvId",
                table: "CandidateCvDownloads");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CandidateCvDownloads",
                table: "CandidateCvDownloads");

            migrationBuilder.RenameTable(
                name: "CandidateCvDownloads",
                newName: "CandidateCvDownload");

            migrationBuilder.RenameIndex(
                name: "IX_CandidateCvDownloads_CandidateCvCvId",
                table: "CandidateCvDownload",
                newName: "IX_CandidateCvDownload_CandidateCvCvId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CandidateCvDownload",
                table: "CandidateCvDownload",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateCvDownload_candidate_cv_CandidateCvCvId",
                table: "CandidateCvDownload",
                column: "CandidateCvCvId",
                principalTable: "candidate_cv",
                principalColumn: "CvId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
