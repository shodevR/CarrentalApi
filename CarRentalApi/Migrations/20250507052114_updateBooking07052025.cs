using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRentalApi.Migrations
{
    /// <inheritdoc />
    public partial class updateBooking07052025 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DriverLicense",
                table: "BookingModify",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverName",
                table: "BookingModify",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverNumber",
                table: "BookingModify",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsInHouse",
                table: "BookingModify",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleName",
                table: "BookingModify",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleNumber",
                table: "BookingModify",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverLicense",
                table: "Booking",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverName",
                table: "Booking",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverNumber",
                table: "Booking",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsInHouse",
                table: "Booking",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleName",
                table: "Booking",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleNumber",
                table: "Booking",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DriverLicense",
                table: "BookingModify");

            migrationBuilder.DropColumn(
                name: "DriverName",
                table: "BookingModify");

            migrationBuilder.DropColumn(
                name: "DriverNumber",
                table: "BookingModify");

            migrationBuilder.DropColumn(
                name: "IsInHouse",
                table: "BookingModify");

            migrationBuilder.DropColumn(
                name: "VehicleName",
                table: "BookingModify");

            migrationBuilder.DropColumn(
                name: "VehicleNumber",
                table: "BookingModify");

            migrationBuilder.DropColumn(
                name: "DriverLicense",
                table: "Booking");

            migrationBuilder.DropColumn(
                name: "DriverName",
                table: "Booking");

            migrationBuilder.DropColumn(
                name: "DriverNumber",
                table: "Booking");

            migrationBuilder.DropColumn(
                name: "IsInHouse",
                table: "Booking");

            migrationBuilder.DropColumn(
                name: "VehicleName",
                table: "Booking");

            migrationBuilder.DropColumn(
                name: "VehicleNumber",
                table: "Booking");
        }
    }
}
