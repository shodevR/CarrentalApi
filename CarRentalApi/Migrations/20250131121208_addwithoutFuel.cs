using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRentalApi.Migrations
{
    /// <inheritdoc />
    public partial class addwithoutFuel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "WithoutFuelAirportDay",
                table: "PriceMaster",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WithoutFuelAirportNight",
                table: "PriceMaster",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WithoutFuelMonthDiscount",
                table: "PriceMaster",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WithoutFuelOutsideCity",
                table: "PriceMaster",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WithoutFuelWeekDiscount",
                table: "PriceMaster",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WithoutFuelWithinCity",
                table: "PriceMaster",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WithoutFuelAirportDay",
                table: "PriceMaster");

            migrationBuilder.DropColumn(
                name: "WithoutFuelAirportNight",
                table: "PriceMaster");

            migrationBuilder.DropColumn(
                name: "WithoutFuelMonthDiscount",
                table: "PriceMaster");

            migrationBuilder.DropColumn(
                name: "WithoutFuelOutsideCity",
                table: "PriceMaster");

            migrationBuilder.DropColumn(
                name: "WithoutFuelWeekDiscount",
                table: "PriceMaster");

            migrationBuilder.DropColumn(
                name: "WithoutFuelWithinCity",
                table: "PriceMaster");
        }
    }
}
