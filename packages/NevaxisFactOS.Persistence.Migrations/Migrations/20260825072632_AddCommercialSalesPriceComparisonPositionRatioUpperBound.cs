using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevaxisFactOS.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddCommercialSalesPriceComparisonPositionRatioUpperBound : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE commercial_sales_price_comparison_results
                    DROP CONSTRAINT IF EXISTS ck_commercial_sales_price_comparison_results_position_ratio;

                ALTER TABLE commercial_sales_price_comparison_results
                    ADD CONSTRAINT ck_commercial_sales_price_comparison_results_position_ratio
                        CHECK (
                            relative_price_position_ratio IS NULL
                            OR (
                                relative_price_position_ratio >= 0
                                AND relative_price_position_ratio < 1));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE commercial_sales_price_comparison_results
                    DROP CONSTRAINT IF EXISTS ck_commercial_sales_price_comparison_results_position_ratio;

                ALTER TABLE commercial_sales_price_comparison_results
                    ADD CONSTRAINT ck_commercial_sales_price_comparison_results_position_ratio
                        CHECK (relative_price_position_ratio IS NULL OR relative_price_position_ratio >= 0);
                """);
        }
    }
}
