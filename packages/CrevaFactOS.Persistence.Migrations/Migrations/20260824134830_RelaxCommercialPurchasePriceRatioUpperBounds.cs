using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrevaFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class RelaxCommercialPurchasePriceRatioUpperBounds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE commercial_purchase_price_assessments
                    DROP CONSTRAINT IF EXISTS ck_commercial_purchase_price_assessments_variance_ratio;

                ALTER TABLE commercial_purchase_price_assessments
                    ADD CONSTRAINT ck_commercial_purchase_price_assessments_variance_ratio
                        CHECK (variance_ratio IS NULL OR variance_ratio > -1);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE commercial_purchase_price_assessments
                    DROP CONSTRAINT IF EXISTS ck_commercial_purchase_price_assessments_variance_ratio;

                ALTER TABLE commercial_purchase_price_assessments
                    ADD CONSTRAINT ck_commercial_purchase_price_assessments_variance_ratio
                        CHECK (variance_ratio IS NULL OR (variance_ratio >= -1 AND variance_ratio <= 1));
                """);
        }
    }
}
