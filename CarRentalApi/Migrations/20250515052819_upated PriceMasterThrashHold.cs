using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRentalApi.Migrations
{
    /// <inheritdoc />
    public partial class upatedPriceMasterThrashHold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "VehicleCategory",
                table: "PriceMaster",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<double>(
                name: "OutsideCityMinimum",
                table: "PriceMaster",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WithinCityMinimum",
                table: "PriceMaster",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WithoutFuelOutsideCityMinimum",
                table: "PriceMaster",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WithoutFuelWithinCityMinimum",
                table: "PriceMaster",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OutsideCityMinimum",
                table: "PriceMaster");

            migrationBuilder.DropColumn(
                name: "WithinCityMinimum",
                table: "PriceMaster");

            migrationBuilder.DropColumn(
                name: "WithoutFuelOutsideCityMinimum",
                table: "PriceMaster");

            migrationBuilder.DropColumn(
                name: "WithoutFuelWithinCityMinimum",
                table: "PriceMaster");

            migrationBuilder.AlterColumn<string>(
                name: "VehicleCategory",
                table: "PriceMaster",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
