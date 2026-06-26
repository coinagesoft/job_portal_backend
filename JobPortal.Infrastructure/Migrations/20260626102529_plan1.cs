using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    public partial class plan1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create index on PlanId
            migrationBuilder.CreateIndex(
                name: "IX_EmployerPlanPurchase_PlanId",
                table: "EmployerPlanPurchase",
                column: "PlanId");

            // Create FK -> CreditPlans
            migrationBuilder.AddForeignKey(
                name: "FK_EmployerPlanPurchase_CreditPlans_PlanId",
                table: "EmployerPlanPurchase",
                column: "PlanId",
                principalTable: "CreditPlans",
                principalColumn: "PlanId",
                onDelete: ReferentialAction.Restrict);

            // Recreate Employer FK with Restrict
            migrationBuilder.DropForeignKey(
                name: "FK_EmployerPlanPurchase_employer_profiles_EmployerId",
                table: "EmployerPlanPurchase");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployerPlanPurchase_employer_profiles_EmployerId",
                table: "EmployerPlanPurchase",
                column: "EmployerId",
                principalTable: "employer_profiles",
                principalColumn: "employer_id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployerPlanPurchase_CreditPlans_PlanId",
                table: "EmployerPlanPurchase");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployerPlanPurchase_employer_profiles_EmployerId",
                table: "EmployerPlanPurchase");

            migrationBuilder.DropIndex(
                name: "IX_EmployerPlanPurchase_PlanId",
                table: "EmployerPlanPurchase");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployerPlanPurchase_employer_profiles_EmployerId",
                table: "EmployerPlanPurchase",
                column: "EmployerId",
                principalTable: "employer_profiles",
                principalColumn: "employer_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}