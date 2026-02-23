using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRentalApi.Migrations
{
    /// <inheritdoc />
    public partial class replacearivalanddestination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "To",
                table: "Booking",
                newName: "Destination");

            migrationBuilder.RenameColumn(
                name: "From",
                table: "Booking",
                newName: "Arival");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Destination",
                table: "Booking",
                newName: "To");

            migrationBuilder.RenameColumn(
                name: "Arival",
                table: "Booking",
                newName: "From");
        }
    }
}
