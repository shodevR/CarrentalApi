using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRentalApi.Migrations
{
    /// <inheritdoc />
    public partial class updatedPriceMasterAndClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AirportTransfer",
                table: "PriceMaster");

            migrationBuilder.DropColumn(
                name: "CorporateDiscount",
                table: "PriceMaster");

            migrationBuilder.DropColumn(
                name: "PerKm",
                table: "PriceMaster");

            migrationBuilder.RenameColumn(
                name: "OneWayTransfer",
                table: "PriceMaster",
                newName: "VehicleCategory");

            migrationBuilder.RenameColumn(
                name: "PassPort",
                table: "Client",
                newName: "ReferedBy");

            migrationBuilder.RenameColumn(
                name: "Document",
                table: "Client",
                newName: "Designation");

            migrationBuilder.AddColumn<string>(
                name: "BusinessProposal",
                table: "Client",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ComapanyType",
                table: "Client",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompanyAddress",
                table: "Client",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "Client",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessProposal",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "ComapanyType",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "CompanyAddress",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "Client");

            migrationBuilder.RenameColumn(
                name: "VehicleCategory",
                table: "PriceMaster",
                newName: "OneWayTransfer");

            migrationBuilder.RenameColumn(
                name: "ReferedBy",
                table: "Client",
                newName: "PassPort");

            migrationBuilder.RenameColumn(
                name: "Designation",
                table: "Client",
                newName: "Document");

            migrationBuilder.AddColumn<string>(
                name: "AirportTransfer",
                table: "PriceMaster",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CorporateDiscount",
                table: "PriceMaster",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "PerKm",
                table: "PriceMaster",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
