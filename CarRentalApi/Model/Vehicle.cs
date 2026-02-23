using System.ComponentModel.DataAnnotations;

namespace CarRentalApi.Model
{
    public class Vehicle
    {
        [Key]
        public int VehicleId { get; set; }
        public string VehicleName { get; set; }
        
        public string Model { get; set; }
        [Required]
        public string VehicleType { get; set; }
        public string MainPhoto { get; set; }
        public string VehicleNumber { get; set; }
        public string ChasisNumber { get; set; }
        public int ManufacturingYear { get; set; }
        public string RegistrationDate { get; set; }
        public string Company { get; set; }
       public string LiveStatus { get; set; }
        public string Fuel { get; set; }
        public int BranchId { get; set; }
        public string RegistrationExpire { get; set; }
        public string VINNo { get; set; }
        public string InsuranceNo { get; set; }
        public string LastServiceDate { get; set; }
        public string Features { get; set; }
        public int TotalTrips { get; set; }
        public int TotalKm { get; set; }
        public int Milage { get; set; }
        public int? CreatedBy { get; set; } 
        public string? CreatedByName { get; set; }
        public bool StatusFlag { get; set;}
    }
}
