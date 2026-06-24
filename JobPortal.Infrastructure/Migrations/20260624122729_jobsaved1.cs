using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class jobsaved1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_saved_jobs_candidate_profiles_candidate_id",
                table: "saved_jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_saved_jobs_job_postings_job_id",
                table: "saved_jobs");

            migrationBuilder.DropIndex(
                name: "IX_saved_jobs_candidate_id",
                table: "saved_jobs");

            migrationBuilder.DropIndex(
                name: "IX_saved_jobs_job_id",
                table: "saved_jobs");

            migrationBuilder.RenameColumn(
                name: "saved_at",
                table: "saved_jobs",
                newName: "SavedAt");

            migrationBuilder.RenameColumn(
                name: "job_id",
                table: "saved_jobs",
                newName: "JobId");

            migrationBuilder.RenameColumn(
                name: "candidate_id",
                table: "saved_jobs",
                newName: "CandidateId");

            migrationBuilder.RenameColumn(
                name: "saved_job_id",
                table: "saved_jobs",
                newName: "SavedJobId");

            migrationBuilder.AddColumn<Guid>(
                name: "CandidateProfileCandidateId",
                table: "saved_jobs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "JobPostingJobId",
                table: "saved_jobs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_saved_jobs_CandidateId_JobId",
                table: "saved_jobs",
                columns: new[] { "CandidateId", "JobId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_saved_jobs_CandidateProfileCandidateId",
                table: "saved_jobs",
                column: "CandidateProfileCandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_saved_jobs_JobPostingJobId",
                table: "saved_jobs",
                column: "JobPostingJobId");

            migrationBuilder.AddForeignKey(
                name: "FK_saved_jobs_candidate_profiles_CandidateProfileCandidateId",
                table: "saved_jobs",
                column: "CandidateProfileCandidateId",
                principalTable: "candidate_profiles",
                principalColumn: "candidate_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_saved_jobs_job_postings_JobPostingJobId",
                table: "saved_jobs",
                column: "JobPostingJobId",
                principalTable: "job_postings",
                principalColumn: "job_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_saved_jobs_candidate_profiles_CandidateProfileCandidateId",
                table: "saved_jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_saved_jobs_job_postings_JobPostingJobId",
                table: "saved_jobs");

            migrationBuilder.DropIndex(
                name: "IX_saved_jobs_CandidateId_JobId",
                table: "saved_jobs");

            migrationBuilder.DropIndex(
                name: "IX_saved_jobs_CandidateProfileCandidateId",
                table: "saved_jobs");

            migrationBuilder.DropIndex(
                name: "IX_saved_jobs_JobPostingJobId",
                table: "saved_jobs");

            migrationBuilder.DropColumn(
                name: "CandidateProfileCandidateId",
                table: "saved_jobs");

            migrationBuilder.DropColumn(
                name: "JobPostingJobId",
                table: "saved_jobs");

            migrationBuilder.RenameColumn(
                name: "SavedAt",
                table: "saved_jobs",
                newName: "saved_at");

            migrationBuilder.RenameColumn(
                name: "JobId",
                table: "saved_jobs",
                newName: "job_id");

            migrationBuilder.RenameColumn(
                name: "CandidateId",
                table: "saved_jobs",
                newName: "candidate_id");

            migrationBuilder.RenameColumn(
                name: "SavedJobId",
                table: "saved_jobs",
                newName: "saved_job_id");

            migrationBuilder.CreateIndex(
                name: "IX_saved_jobs_candidate_id",
                table: "saved_jobs",
                column: "candidate_id");

            migrationBuilder.CreateIndex(
                name: "IX_saved_jobs_job_id",
                table: "saved_jobs",
                column: "job_id");

            migrationBuilder.AddForeignKey(
                name: "FK_saved_jobs_candidate_profiles_candidate_id",
                table: "saved_jobs",
                column: "candidate_id",
                principalTable: "candidate_profiles",
                principalColumn: "candidate_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_saved_jobs_job_postings_job_id",
                table: "saved_jobs",
                column: "job_id",
                principalTable: "job_postings",
                principalColumn: "job_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
