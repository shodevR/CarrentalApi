using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;

namespace CarRentalApi.Model
{
    namespace CarRentalApi.Model
    {
        public class BookingModify : BaseModify
        {
            [Key]
            public int BookingModifiedId { get; set; }
            
            public int BookingId { get; set; }
            public int VehicleId { get; set; }
            public string? VehicleName { get; set; }
            public string? VehicleNumber { get; set; }
            public int BranchId { get; set; }
            public int? DriverId { get; set; }
            public string? DriverName { get; set; }
            public string? DriverNumber { get; set; }
            public string? DriverLicense { get; set; }


            public int? ClientId { get; set; }
            public int? ExistingClientId { get; set; }
            public string? ExistingClientName { get; set; }

            public DateTime? Date { get; set; }
            public DateTime BookingDateFrom { get; set; }
            public DateTime BookingDateTo { get; set; }
            public string? Destination { get; set; }
            public string? Arival { get; set; }
            public string? PaymentOption { get; set; }
            public bool? WithDriver { get; set; }
            public decimal? DriverPrice { get; set; }
			public decimal? VehiclePrice { get; set; }
			public string? VehicleImage { get; set; }
			public string? DriverImage { get; set; }
			public string? TripType { get; set; }
            public string? ServiceTime { get; set; }
            public decimal? Discount { get; set; }
            public decimal? Amount { get; set; }
			public string? CurrencyCode { get; set; }
			public decimal? ROI { get; set; }

			public bool? WithFuel { get; set; }
            public string? Document { get; set; }
            public int? CreatedBy { get; set; }
            public bool? IsInHouse { get; set; } = true;
            public string? CreatedByName { get; set; }
            public bool? StatusFlag { get; set; }
        }
    }

}
