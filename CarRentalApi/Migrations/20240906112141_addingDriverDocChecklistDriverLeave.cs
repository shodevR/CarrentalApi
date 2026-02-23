using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRentalApi.Migrations
{
    /// <inheritdoc />
    public partial class addingDriverDocChecklistDriverLeave : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CheckList",
                columns: table => new
                {
                    CheckListId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    OdometerBefore = table.Column<int>(type: "int", nullable: false),
                    OdometerAfter = table.Column<int>(type: "int", nullable: false),
                    FuelBefore = table.Column<int>(type: "int", nullable: false),
                    FuelAfter = table.Column<int>(type: "int", nullable: false),
                    TyrePressureBefore = table.Column<int>(type: "int", nullable: false),
                    TyrePressureAfter = table.Column<int>(type: "int", nullable: false),
                    CarToolsBefore = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CarToolsAfter = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SafetyEquipmentsBefore = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SafetyEquipmentsAfter = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOfReadingBefore = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateOfReadingAfter = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckList", x => x.CheckListId);
                });

            migrationBuilder.CreateTable(
                name: "DriverDocument",
                columns: table => new
                {
                    DriverDocuId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DriverId = table.Column<int>(type: "int", nullable: false),
                    LicensePlate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LicenseExpDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NationalId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NationalIdExpDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OtherDocument = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OtherDocumentExpDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    upload = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverDocument", x => x.DriverDocuId);
                });

            migrationBuilder.CreateTable(
                name: "DriverLeave",
                columns: table => new
                {
                    DriverLeaveId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DriverId = table.Column<int>(type: "int", nullable: false),
                    DriverName = table.Column<int>(type: "int", nullable: false),
                    LeaveDateFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeaveDateTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverLeave", x => x.DriverLeaveId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CheckList");

            migrationBuilder.DropTable(
                name: "DriverDocument");

            migrationBuilder.DropTable(
                name: "DriverLeave");
        }
    }
}
