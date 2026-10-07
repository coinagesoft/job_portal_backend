using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class coupanentities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CouponCode",
                table: "payment_transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CouponId",
                table: "payment_transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiscountAmountPaise",
                table: "payment_transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Region",
                table: "MembershipPlans",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "MembershipPlans",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "PlanName",
                table: "MembershipPlans",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Period",
                table: "MembershipPlans",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Badge",
                table: "MembershipPlans",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "coupons",
                columns: table => new
                {
                    coupon_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    plan_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    region = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    discount_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    minimum_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    maximum_discount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    usage_limit = table.Column<int>(type: "integer", nullable: true),
                    per_user_limit = table.Column<int>(type: "integer", nullable: true),
                    start_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    used_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coupons", x => x.coupon_id);
                    table.ForeignKey(
                        name: "FK_coupons_MembershipPlans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "MembershipPlans",
                        principalColumn: "PlanId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "coupon_redemptions",
                columns: table => new
                {
                    redemption_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coupon_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    coupon_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    original_amount_paise = table.Column<int>(type: "integer", nullable: false),
                    discount_amount_paise = table.Column<int>(type: "integer", nullable: false),
                    final_amount_paise = table.Column<int>(type: "integer", nullable: false),
                    redeemed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coupon_redemptions", x => x.redemption_id);
                    table.ForeignKey(
                        name: "FK_coupon_redemptions_MembershipPlans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "MembershipPlans",
                        principalColumn: "PlanId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_coupon_redemptions_coupons_coupon_id",
                        column: x => x.coupon_id,
                        principalTable: "coupons",
                        principalColumn: "coupon_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_coupon_redemptions_payment_transactions_payment_transaction~",
                        column: x => x.payment_transaction_id,
                        principalTable: "payment_transactions",
                        principalColumn: "TransactionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_coupon_redemptions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payment_transactions_coupon_code",
                table: "payment_transactions",
                column: "CouponCode");

            migrationBuilder.CreateIndex(
                name: "ix_payment_transactions_coupon_id",
                table: "payment_transactions",
                column: "CouponId");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPlans_PlanType_Region_IsActive",
                table: "MembershipPlans",
                columns: new[] { "PlanType", "Region", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "ix_coupon_redemptions_coupon_id",
                table: "coupon_redemptions",
                column: "coupon_id");

            migrationBuilder.CreateIndex(
                name: "ix_coupon_redemptions_coupon_user",
                table: "coupon_redemptions",
                columns: new[] { "coupon_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_coupon_redemptions_plan_id",
                table: "coupon_redemptions",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_coupon_redemptions_redeemed_at",
                table: "coupon_redemptions",
                column: "redeemed_at");

            migrationBuilder.CreateIndex(
                name: "ix_coupon_redemptions_user_id",
                table: "coupon_redemptions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uq_coupon_redemptions_payment_transaction",
                table: "coupon_redemptions",
                column: "payment_transaction_id",
                unique: true,
                filter: "\"payment_transaction_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_coupons_plan_id",
                table: "coupons",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_coupons_plan_type_region_active",
                table: "coupons",
                columns: new[] { "plan_type", "region", "is_active" });

            migrationBuilder.CreateIndex(
                name: "uq_coupons_code",
                table: "coupons",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_payment_transactions_coupons_CouponId",
                table: "payment_transactions",
                column: "CouponId",
                principalTable: "coupons",
                principalColumn: "coupon_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payment_transactions_coupons_CouponId",
                table: "payment_transactions");

            migrationBuilder.DropTable(
                name: "coupon_redemptions");

            migrationBuilder.DropTable(
                name: "coupons");

            migrationBuilder.DropIndex(
                name: "ix_payment_transactions_coupon_code",
                table: "payment_transactions");

            migrationBuilder.DropIndex(
                name: "ix_payment_transactions_coupon_id",
                table: "payment_transactions");

            migrationBuilder.DropIndex(
                name: "IX_MembershipPlans_PlanType_Region_IsActive",
                table: "MembershipPlans");

            migrationBuilder.DropColumn(
                name: "CouponCode",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "CouponId",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "DiscountAmountPaise",
                table: "payment_transactions");

            migrationBuilder.AlterColumn<string>(
                name: "Region",
                table: "MembershipPlans",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "MembershipPlans",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "PlanName",
                table: "MembershipPlans",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Period",
                table: "MembershipPlans",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Badge",
                table: "MembershipPlans",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}
