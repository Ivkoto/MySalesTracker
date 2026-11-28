using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySalesTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PriceRules_ProductId",
                table: "PriceRules");

            migrationBuilder.DropIndex(
                name: "IX_PriceRules_ProductId_Price",
                table: "PriceRules");

            migrationBuilder.AddColumn<int>(
                name: "PriceList",
                table: "PriceRules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PriceList",
                table: "Events",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PriceRules_ProductId_PriceList",
                table: "PriceRules",
                columns: new[] { "ProductId", "PriceList" },
                unique: true,
                filter: "[IsDefault] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PriceRules_ProductId_PriceList_Price",
                table: "PriceRules",
                columns: new[] { "ProductId", "PriceList", "Price" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM Events WHERE PriceList <> 0)
                    OR EXISTS (SELECT 1 FROM PriceRules WHERE PriceList <> 0)
                BEGIN
                    ;THROW 50010, N'Rollback of AddPriceLists would discard EU price-list data.', 1;
                END
                """);

            migrationBuilder.DropIndex(
                name: "IX_PriceRules_ProductId_PriceList",
                table: "PriceRules");

            migrationBuilder.DropIndex(
                name: "IX_PriceRules_ProductId_PriceList_Price",
                table: "PriceRules");

            migrationBuilder.DropColumn(
                name: "PriceList",
                table: "PriceRules");

            migrationBuilder.DropColumn(
                name: "PriceList",
                table: "Events");

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
    }
}
