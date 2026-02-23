using System.ComponentModel.DataAnnotations;

namespace CarRentalApi.Model
{
    public class Driver
    {
        [Key]
        public int DriverId { get; set; }
        public int BranchId { get; set; }
        public string? DriverName { get; set; }
        public int TripsCovered { get; set; }
        public string? LiveStatus { get; set; }
        public int Experience { get; set; }
        public string? Contact { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public int Age { get; set; }
        public bool StatusFlag { get; set; }
        public string? LicensePlate { get; set; }
        public int Salary { get; set; }
        public string? NationalId { get; set; }
        public string? EmergencyNumber { get; set; }
        public string? Image { get; set; }
        public string? VerificationDocument { get; set; }
        public int VerificationStatus { get; set; }
        public string Expertise { get; set; }
        public decimal PricePerDay { get; set; }
        public int? CreatedBy { get; set; } 
        public string? CreatedByName { get; set; }
        public string? LicenseExp { get; set; }
    }
}
