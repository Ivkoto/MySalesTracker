using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySalesTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class renameUnitPricetoPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "Sale",
                newName: "Price");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Sale",
                newName: "UnitPrice");
        }
    }
}
