using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRentalApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceMasterToDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PriceMaster",
                columns: table => new
                {
                    PriceMasterId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    WithinCity = table.Column<double>(type: "float", nullable: false),
                    OutsideCity = table.Column<double>(type: "float", nullable: false),
                    PerKm = table.Column<double>(type: "float", nullable: false),
                    AirportTransfer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OneWayTransfer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorporateDiscount = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceMaster", x => x.PriceMasterId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceMaster");
        }
    }
}
