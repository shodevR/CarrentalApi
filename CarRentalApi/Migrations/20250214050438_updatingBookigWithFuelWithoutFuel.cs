using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRentalApi.Migrations
{
    /// <inheritdoc />
    public partial class updatingBookigWithFuelWithoutFuel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DestinationType",
                table: "BookingModify");

            migrationBuilder.DropColumn(
                name: "invoice",
                table: "BookingModify");

            migrationBuilder.DropColumn(
                name: "DestinationType",
                table: "Booking");

            migrationBuilder.DropColumn(
                name: "invoice",
                table: "Booking");

            migrationBuilder.AddColumn<bool>(
                name: "WithDriver",
                table: "BookingModify",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WithFuel",
                table: "BookingModify",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WithDriver",
                table: "Booking",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WithFuel",
                table: "Booking",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WithDriver",
                table: "BookingModify");

            migrationBuilder.DropColumn(
                name: "WithFuel",
                table: "BookingModify");

            migrationBuilder.DropColumn(
                name: "WithDriver",
                table: "Booking");

            migrationBuilder.DropColumn(
                name: "WithFuel",
                table: "Booking");

            migrationBuilder.AddColumn<string>(
                name: "DestinationType",
                table: "BookingModify",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "invoice",
                table: "BookingModify",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestinationType",
                table: "Booking",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "invoice",
                table: "Booking",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
