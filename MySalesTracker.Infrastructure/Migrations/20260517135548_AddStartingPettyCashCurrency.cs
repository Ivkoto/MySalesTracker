using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySalesTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStartingPettyCashCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StartingPettyCashCurrency",
                table: "EventDays",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartingPettyCashCurrency",
                table: "EventDays");
        }
    }
}
