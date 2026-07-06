using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    public partial class jobsaved : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename columns
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

            // Rename existing unique index
            migrationBuilder.RenameIndex(
                name: "IX_saved_jobs_CandidateId_JobId",
                table: "saved_jobs",
                newName: "IX_saved_jobs_candidate_id_job_id");

            // Create index on JobId
            migrationBuilder.CreateIndex(
                name: "IX_saved_jobs_job_id",
                table: "saved_jobs",
                column: "job_id");

            // Correct FK to Candidate
            migrationBuilder.AddForeignKey(
                name: "FK_saved_jobs_candidate_profiles_candidate_id",
                table: "saved_jobs",
                column: "candidate_id",
                principalTable: "candidate_profiles",
                principalColumn: "candidate_id",
                onDelete: ReferentialAction.Cascade);

            // Correct FK to Job
            migrationBuilder.AddForeignKey(
                name: "FK_saved_jobs_job_postings_job_id",
                table: "saved_jobs",
                column: "job_id",
                principalTable: "job_postings",
                principalColumn: "job_id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_saved_jobs_candidate_profiles_candidate_id",
                table: "saved_jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_saved_jobs_job_postings_job_id",
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

            migrationBuilder.RenameIndex(
                name: "IX_saved_jobs_candidate_id_job_id",
                table: "saved_jobs",
                newName: "IX_saved_jobs_CandidateId_JobId");
        }
    }
}