using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixCandidateCvDownloadForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CandidateCvDownloads_candidate_cv_CandidateCvCvId",
                table: "CandidateCvDownloads");

            migrationBuilder.DropIndex(
                name: "IX_CandidateCvDownloads_CandidateCvCvId",
                table: "CandidateCvDownloads");

            migrationBuilder.DropColumn(
                name: "CandidateCvCvId",
                table: "CandidateCvDownloads");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateCvDownloads_CvId",
                table: "CandidateCvDownloads",
                column: "CvId");

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateCvDownloads_candidate_cv_CvId",
                table: "CandidateCvDownloads",
                column: "CvId",
                principalTable: "candidate_cv",
                principalColumn: "CvId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CandidateCvDownloads_candidate_cv_CvId",
                table: "CandidateCvDownloads");

            migrationBuilder.DropIndex(
                name: "IX_CandidateCvDownloads_CvId",
                table: "CandidateCvDownloads");

            migrationBuilder.AddColumn<Guid>(
                name: "CandidateCvCvId",
                table: "CandidateCvDownloads",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_CandidateCvDownloads_CandidateCvCvId",
                table: "CandidateCvDownloads",
                column: "CandidateCvCvId");

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateCvDownloads_candidate_cv_CandidateCvCvId",
                table: "CandidateCvDownloads",
                column: "CandidateCvCvId",
                principalTable: "candidate_cv",
                principalColumn: "CvId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
