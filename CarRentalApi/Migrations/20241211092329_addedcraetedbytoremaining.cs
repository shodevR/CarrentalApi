using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRentalApi.Migrations
{
    /// <inheritdoc />
    public partial class addedcraetedbytoremaining : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "VehicleMaintenance",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "DriverLeave",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByAfter",
                table: "CheckList",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByBefore",
                table: "CheckList",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "VehicleMaintenance");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "DriverLeave");

            migrationBuilder.DropColumn(
                name: "CreatedByAfter",
                table: "CheckList");

            migrationBuilder.DropColumn(
                name: "CreatedByBefore",
                table: "CheckList");
        }
    }
}
