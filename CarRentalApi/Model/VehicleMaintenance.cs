using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;

namespace CarRentalApi.Model
{
    public class VehicleMaintenance
    {
        [Key]
        public int VehicleMaintenanceId { get; set; }
        public int BranchId { get; set; }
        public DateTime StartDate { get; set; }
        public Decimal Cost { get; set; }
        public DateTime? NextDate { get; set; }
        public DateTime? ExpectedReturnDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string? Reason { get; set; }
        public string? Description { get; set; }
        public string? GarageName { get; set; }
        public int VehicleId { get; set; }
        public int? KmIn {  get; set; }
        public int? KmOut { get; set; }
        public string? MaintenanceIssuedBy { get; set; }
        public int? DriverId { get; set; }
        public int? CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
        public bool StatusFlag { get; set; }
    }
}
