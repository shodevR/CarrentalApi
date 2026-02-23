using System.ComponentModel.DataAnnotations;

namespace CarRentalApi.Model
{
    public class PriceMaster
    {
        [Key]
        public int PriceMasterId {  get; set; }
        public int VehicleId { get; set; }
        public string? VehicleCategory { get; set; }

        public double WithinCity { get; set; }
        public double OutsideCity { get; set; }
        public double? WithinCityMinimum { get; set; } = 0;
		public double? OutsideCityMinimum { get; set; } = 0;
		public double? WithoutFuelWithinCityMinimum { get; set; } = 0;
		public double? WithoutFuelOutsideCityMinimum { get; set; } = 0;
		public double? WithinCityExtraHoursCharges { get; set; } = 0;
		public double? OutsideCityExtraHoursCharges { get; set; } = 0;
		public double? WithoutFuelWithinExtraHoursCharges { get; set; } = 0;
		public double? WithoutFuelOutsideExtraHoursCharges { get; set; } = 0;
		public decimal? WeekDiscount { get; set; }
        public decimal? MonthDiscount { get; set; }
        public double? AirportDay { get; set; }
        public double? AirportNight { get; set; }

        public double? WithoutFuelWithinCity { get; set; }
        public double? WithoutFuelOutsideCity { get; set; }
        public decimal? WithoutFuelWeekDiscount { get; set; }
        public decimal? WithoutFuelMonthDiscount { get; set; }
        public double? WithoutFuelAirportDay { get; set; }
        public double? WithoutFuelAirportNight { get; set; }
        

		public int? CreatedBy { get; set; } 
        public string? CreatedByName { get; set; }
        public DateTime? CreatedOn { get; set; } = DateTime.Now;

        public bool StatusFlag { get; set; }



    }
}
