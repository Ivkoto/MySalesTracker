using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySalesTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyPriceRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sale_PriceRules_PriceRuleId",
                table: "Sale");

            migrationBuilder.DropIndex(
                name: "IX_Sale_PriceRuleId",
                table: "Sale");

            migrationBuilder.DropIndex(
                name: "IX_PriceRules_ProductId_EffectiveFrom",
                table: "PriceRules");

            migrationBuilder.DropIndex(
                name: "IX_PriceRules_ProductId_Price",
                table: "PriceRules");

            migrationBuilder.DropColumn(
                name: "PriceRuleId",
                table: "Sale");

            migrationBuilder.Sql(
                """
                DECLARE @Today date = CAST(GETDATE() AS date);

                WITH ActiveCandidates AS
                (
                    SELECT
                        priceRule.PriceRuleId,
                        priceRule.ProductId,
                        priceRule.Price,
                        priceRule.EffectiveFrom
                    FROM PriceRules AS priceRule
                    WHERE priceRule.EffectiveFrom <= @Today
                        AND (priceRule.EffectiveTo IS NULL OR priceRule.EffectiveTo >= @Today)
                ),
                LatestFallbackDates AS
                (
                    SELECT
                        priceRule.ProductId,
                        MAX(priceRule.EffectiveFrom) AS EffectiveFrom
                    FROM PriceRules AS priceRule
                    WHERE NOT EXISTS
                    (
                        SELECT 1
                        FROM ActiveCandidates AS activeRule
                        WHERE activeRule.ProductId = priceRule.ProductId
                    )
                    GROUP BY priceRule.ProductId
                ),
                Candidates AS
                (
                    SELECT
                        activeRule.PriceRuleId,
                        activeRule.ProductId,
                        activeRule.Price,
                        activeRule.EffectiveFrom
                    FROM ActiveCandidates AS activeRule

                    UNION ALL

                    SELECT
                        priceRule.PriceRuleId,
                        priceRule.ProductId,
                        priceRule.Price,
                        priceRule.EffectiveFrom
                    FROM PriceRules AS priceRule
                    INNER JOIN LatestFallbackDates AS fallback
                        ON fallback.ProductId = priceRule.ProductId
                        AND fallback.EffectiveFrom = priceRule.EffectiveFrom
                ),
                RankedCandidates AS
                (
                    SELECT
                        candidate.PriceRuleId,
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY candidate.ProductId, candidate.Price
                            ORDER BY candidate.EffectiveFrom DESC, candidate.PriceRuleId DESC
                        ) AS RowNumber
                    FROM Candidates AS candidate
                )
                DELETE priceRule
                FROM PriceRules AS priceRule
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM RankedCandidates AS candidate
                    WHERE candidate.PriceRuleId = priceRule.PriceRuleId
                        AND candidate.RowNumber = 1
                );

                WITH RankedDefaults AS
                (
                    SELECT
                        priceRule.PriceRuleId,
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY priceRule.ProductId
                            ORDER BY
                                CASE WHEN priceRule.IsDefault = 1 THEN 0 ELSE 1 END,
                                priceRule.SortOrder,
                                priceRule.PriceRuleId
                        ) AS RowNumber
                    FROM PriceRules AS priceRule
                )
                UPDATE priceRule
                SET IsDefault = CASE WHEN ranked.RowNumber = 1 THEN 1 ELSE 0 END
                FROM PriceRules AS priceRule
                INNER JOIN RankedDefaults AS ranked
                    ON ranked.PriceRuleId = priceRule.PriceRuleId;
                """);

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                table: "PriceRules");

            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                table: "PriceRules");

            migrationBuilder.CreateIndex(
                name: "IX_PriceRules_ProductId",
                table: "PriceRules",
                column: "ProductId",
                unique: true,
                filter: "[IsDefault] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PriceRules_ProductId_Price",
                table: "PriceRules",
                columns: new[] { "ProductId", "Price" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PriceRules_ProductId",
                table: "PriceRules");

            migrationBuilder.DropIndex(
                name: "IX_PriceRules_ProductId_Price",
                table: "PriceRules");

            migrationBuilder.AddColumn<int>(
                name: "PriceRuleId",
                table: "Sale",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EffectiveFrom",
                table: "PriceRules",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "EffectiveTo",
                table: "PriceRules",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sale_PriceRuleId",
                table: "Sale",
                column: "PriceRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceRules_ProductId_EffectiveFrom",
                table: "PriceRules",
                columns: new[] { "ProductId", "EffectiveFrom" },
                unique: true,
                filter: "[IsDefault] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PriceRules_ProductId_Price",
                table: "PriceRules",
                columns: new[] { "ProductId", "Price" });

            migrationBuilder.AddForeignKey(
                name: "FK_Sale_PriceRules_PriceRuleId",
                table: "Sale",
                column: "PriceRuleId",
                principalTable: "PriceRules",
                principalColumn: "PriceRuleId");
        }
    }
}
