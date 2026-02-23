using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRentalApi.Migrations
{
    /// <inheritdoc />
    public partial class updateStatusFlagToSomeClasses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "StatusFlag",
                table: "VehicleMaintenance",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "StatusFlag",
                table: "VehicleDocument",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "StatusFlag",
                table: "PriceMaster",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "StatusFlag",
                table: "DriverLeave",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "StatusFlag",
                table: "Client",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "StatusFlag",
                table: "CheckList",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "StatusFlag",
                table: "Booking",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StatusFlag",
                table: "VehicleMaintenance");

            migrationBuilder.DropColumn(
                name: "StatusFlag",
                table: "VehicleDocument");

            migrationBuilder.DropColumn(
                name: "StatusFlag",
                table: "PriceMaster");

            migrationBuilder.DropColumn(
                name: "StatusFlag",
                table: "DriverLeave");

            migrationBuilder.DropColumn(
                name: "StatusFlag",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "StatusFlag",
                table: "CheckList");

            migrationBuilder.DropColumn(
                name: "StatusFlag",
                table: "Booking");
        }
    }
}
