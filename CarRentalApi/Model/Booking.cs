using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarRentalApi.Model
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }
        public int VehicleId { get; set; } = 0;
        public string? VehicleName { get; set; }
        public string? VehicleNumber { get; set; }
        public int BranchId { get; set; }
        public int? DriverId { get; set; } = 0;
		public string? DriverName { get; set; }
        public string?DriverNumber { get; set; }
        public string?DriverLicense { get; set; }

       

        public int? ClientId { get; set; } = 0;
       public int? ExistingClientId { get; set; } = 0;
        public string? ExistingClientName { get; set; }

        public DateTime? Date { get; set; }
        public DateTime BookingDateFrom { get; set; }
        public DateTime BookingDateTo { get; set; }
        public string? Destination { get; set; }
        public string? Arival { get; set; }
        public string? PaymentOption { get; set; }
        public bool? WithDriver { get; set; }
        public decimal? DriverPrice { get; set; } = 0;
        public string ? TripType { get; set; }
        public string? ServiceTime { get; set; }
        public bool? IsInHouse { get; set; } = true;
        public decimal? VehiclePrice { get; set; } = 0;
		public decimal? Discount { get; set; }
        public decimal? ExtraCharges { get; set; } = 0;
        public decimal? Amount { get; set; } = 0;
		public bool? WithFuel { get; set; }
        public string? Document { get; set; }
        public int? CreatedBy { get; set; } 
        public string? CreatedByName { get; set; }
        public string? VehicleImage { get; set; }
        public string? DriverImage { get; set; }
        public string? CurrencyCode { get; set; }
		[Column(TypeName = "decimal(18,8)")]
		public decimal? ROI { get; set; }
        
        public bool? StatusFlag { get; set; }

    }
}