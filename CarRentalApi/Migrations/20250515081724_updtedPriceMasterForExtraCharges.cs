using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRentalApi.Migrations
{
    /// <inheritdoc />
    public partial class updtedPriceMasterForExtraCharges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "OutsideCityExtraHoursCharges",
                table: "PriceMaster",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WithinCityExtraHoursCharges",
                table: "PriceMaster",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WithoutFuelOutsideExtraHoursCharges",
                table: "PriceMaster",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WithoutFuelWithinExtraHoursCharges",
                table: "PriceMaster",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ExtraHours",
                table: "CheckList",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExtraCharges",
                table: "Booking",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OutsideCityExtraHoursCharges",
                table: "PriceMaster");

            migrationBuilder.DropColumn(
                name: "WithinCityExtraHoursCharges",
                table: "PriceMaster");

            migrationBuilder.DropColumn(
                name: "WithoutFuelOutsideExtraHoursCharges",
                table: "PriceMaster");

            migrationBuilder.DropColumn(
                name: "WithoutFuelWithinExtraHoursCharges",
                table: "PriceMaster");

            migrationBuilder.DropColumn(
                name: "ExtraHours",
                table: "CheckList");

            migrationBuilder.DropColumn(
                name: "ExtraCharges",
                table: "Booking");
        }
    }
}
