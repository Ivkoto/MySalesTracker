using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySalesTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceRuleDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "PriceRules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                WITH EffectiveRules AS
                (
                    SELECT
                        priceRule.PriceRuleId,
                        priceRule.ProductId,
                        priceRule.EffectiveFrom,
                        priceRule.SortOrder,
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY priceRule.ProductId, priceRule.Price
                            ORDER BY
                                priceRule.EffectiveFrom DESC,
                                priceRule.SortOrder,
                                priceRule.PriceRuleId DESC
                        ) AS VersionNumber
                    FROM PriceRules AS priceRule
                    WHERE priceRule.EffectiveFrom <= CAST(GETDATE() AS date)
                        AND (priceRule.EffectiveTo IS NULL OR priceRule.EffectiveTo >= CAST(GETDATE() AS date))
                ),
                RankedRules AS
                (
                    SELECT
                        priceRule.PriceRuleId,
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY priceRule.ProductId
                            ORDER BY
                                priceRule.SortOrder,
                                priceRule.EffectiveFrom DESC,
                                priceRule.PriceRuleId
                        ) AS RowNumber
                    FROM EffectiveRules AS priceRule
                    WHERE priceRule.VersionNumber = 1
                )
                UPDATE priceRule
                SET IsDefault = 1
                FROM PriceRules AS priceRule
                INNER JOIN RankedRules AS ranked ON ranked.PriceRuleId = priceRule.PriceRuleId
                WHERE ranked.RowNumber = 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_PriceRules_ProductId_EffectiveFrom",
                table: "PriceRules",
                columns: new[] { "ProductId", "EffectiveFrom" },
                unique: true,
                filter: "[IsDefault] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PriceRules_ProductId_EffectiveFrom",
                table: "PriceRules");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "PriceRules");
        }
    }
}
