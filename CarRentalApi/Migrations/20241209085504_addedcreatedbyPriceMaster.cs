using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRentalApi.Migrations
{
    /// <inheritdoc />
    public partial class addedcreatedbyPriceMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AirportDay",
                table: "PriceMaster",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AirportNight",
                table: "PriceMaster",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "PriceMaster",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "PriceMaster",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthDiscount",
                table: "PriceMaster",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeekDiscount",
                table: "PriceMaster",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AirportDay",
                table: "PriceMaster");

            migrationBuilder.DropColumn(
                name: "AirportNight",
                table: "PriceMaster");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "PriceMaster");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "PriceMaster");

            migrationBuilder.DropColumn(
                name: "MonthDiscount",
                table: "PriceMaster");

            migrationBuilder.DropColumn(
                name: "WeekDiscount",
                table: "PriceMaster");
        }
    }
}
