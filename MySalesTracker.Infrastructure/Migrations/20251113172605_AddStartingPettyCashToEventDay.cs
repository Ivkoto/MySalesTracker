using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySalesTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStartingPettyCashToEventDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "StartingPettyCash",
                table: "EventDays",
                type: "decimal(8,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartingPettyCash",
                table: "EventDays");
        }
    }
}
