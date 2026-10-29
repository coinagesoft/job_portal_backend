using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using JobPortal.Infrastructure.Persistence;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <summary>
    /// PaymentTransaction.PayPalOrderId / PayPalCaptureId were added to the
    /// entity (PayPal checkout) but no migration ever created the columns, so
    /// every query/insert on payment_transactions failed with
    /// 42703: column "PayPalCaptureId" ... does not exist.
    ///
    /// IF NOT EXISTS keeps this safe if someone already added them by hand.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261021090000_AddPayPalColumnsToPaymentTransactions")]
    public partial class AddPayPalColumnsToPaymentTransactions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE payment_transactions
                    ADD COLUMN IF NOT EXISTS ""PayPalOrderId""   text NULL,
                    ADD COLUMN IF NOT EXISTS ""PayPalCaptureId"" text NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE payment_transactions
                    DROP COLUMN IF EXISTS ""PayPalCaptureId"",
                    DROP COLUMN IF EXISTS ""PayPalOrderId"";
            ");
        }
    }
}