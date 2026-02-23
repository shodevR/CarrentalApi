using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRentalApi.Migrations
{
    /// <inheritdoc />
    public partial class updatedCreatedBynameField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "VehicleMaintenance",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "Vehicle",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "PriceMaster",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "DriverLeave",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMailSent",
                table: "DriverDocuments",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "Driver",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMailSent",
                table: "Document",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "Client",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "BookingModify",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "Booking",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "VehicleMaintenance");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "Vehicle");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "PriceMaster");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "DriverLeave");

            migrationBuilder.DropColumn(
                name: "IsMailSent",
                table: "DriverDocuments");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "Driver");

            migrationBuilder.DropColumn(
                name: "IsMailSent",
                table: "Document");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "BookingModify");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "Booking");
        }
    }
}
