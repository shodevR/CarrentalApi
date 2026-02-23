using CarRentalApi.Data;
using CarRentalApi.Model;
using CarRentalApi.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CarRentalApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PriceMasterController :BaseController
    {
		
		private readonly ApplicationDbContext _db;
		private readonly ICurrencyConversionService _currencyService;

		public PriceMasterController(ApplicationDbContext db
		  , IHttpContextAccessor contextAccessor, IConfiguration configuration, IWebHostEnvironment hostingEnvironment, ICurrencyConversionService currencyService)
			  : base(hostingEnvironment, contextAccessor, configuration, db)
		{
			_db = db;
			_currencyService = currencyService;
			

		}

		[HttpGet]
		public async Task<IActionResult> GetPriceMasters(
	[FromQuery] string searchText = "",
	[FromQuery] int vehicleId = 0,
	[FromQuery] int branchId = 0,
	[FromQuery] int currentPageNumber = 1,
	[FromQuery] int pageSize = 50,
	[FromQuery] string toCurrency = "USD")
		{
			// Join PriceMaster with Vehicle and Branch
			var query = from pm in _db.PriceMaster
						join v in _db.Vehicle on pm.VehicleId equals v.VehicleId
						join b in _db.LocationMaster on v.BranchId equals b.Id into branchJoin
						from b in branchJoin.DefaultIfEmpty()
						select new
						{
							PriceMaster = pm,
							Vehicle = v,
							Branch = b,
							OriginalCurrency = b != null ? b.CurrencyCode ?? "USD" : "USD"
						};

			// Apply search functionality
			if (!string.IsNullOrEmpty(searchText))
			{
				query = query.Where(p =>
					p.PriceMaster.VehicleCategory.Contains(searchText) ||
					p.Vehicle.VehicleName.Contains(searchText) ||
					p.Vehicle.VehicleNumber.Contains(searchText));
			}

			// Filter by VehicleId
			if (vehicleId > 0)
			{
				query = query.Where(p => p.Vehicle.VehicleId == vehicleId);
			}

			// Filter by BranchId
			if (branchId > 0)
			{
				query = query.Where(p => p.Vehicle.BranchId == branchId);
			}

			// Get distinct currencies from the results
			var currencies = await query
				.Select(p => p.OriginalCurrency)
				.Distinct()
				.ToListAsync();

			// Get conversion rates for unique currencies
			var conversionRates = new Dictionary<string, decimal>();
			foreach (var currency in currencies)
			{
				if (currency != toCurrency)
				{
					conversionRates[currency] = await _currencyService.GetConversionRateAsync(currency, toCurrency);
				}
			}

			// Sort in descending order by PriceMasterId
			query = query.OrderByDescending(p => p.PriceMaster.PriceMasterId);

			// Pagination
			int totalItems = await query.CountAsync();
			int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
			var priceMasters = await query
				.Skip((currentPageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			// Convert prices to target currency
			var convertedPriceMasters = priceMasters.Select(p =>
			{
				var pm = p.PriceMaster;
				var rate = p.OriginalCurrency == toCurrency
					? 1.0m
					: conversionRates.GetValueOrDefault(p.OriginalCurrency, 1.0m);

				return new
				{
					pm.PriceMasterId,
					pm.VehicleId,
					p.Vehicle.VehicleName,
					p.Vehicle.VehicleNumber,
					p.Vehicle.BranchId,
					pm.VehicleCategory,
					WithinCity = Math.Round(pm.WithinCity * (double)rate, 2),
					OutsideCity = Math.Round(pm.OutsideCity * (double)rate, 2),
					pm.WeekDiscount,
					pm.MonthDiscount,
					AirportDay = Math.Round((pm.AirportDay ?? 0) * (double)rate, 2),
					AirportNight = Math.Round((pm.AirportNight ?? 0) * (double)rate, 2),
					WithoutFuelWithinCity = Math.Round((pm.WithoutFuelWithinCity ?? 0) * (double)rate, 2),
					WithoutFuelOutsideCity = Math.Round((pm.WithoutFuelOutsideCity ?? 0) * (double)rate, 2),
					pm.WithoutFuelWeekDiscount,
					pm.WithoutFuelMonthDiscount,
					WithoutFuelAirportDay = Math.Round((pm.WithoutFuelAirportDay ?? 0) * (double)rate, 2),
					WithoutFuelAirportNight = Math.Round((pm.WithoutFuelAirportNight ?? 0) * (double)rate, 2),
					WithinCityMinimum = Math.Round((pm.WithinCityMinimum ?? 0) * (double)rate, 2),
					OutsideCityMinimum = Math.Round((pm.OutsideCityMinimum ?? 0) * (double)rate, 2),
					WithoutFuelWithinCityMinimum = Math.Round((pm.WithoutFuelWithinCityMinimum ?? 0) * (double)rate, 2),
					WithoutFuelOutsideCityMinimum = Math.Round((pm.WithoutFuelOutsideCityMinimum ?? 0) * (double)rate, 2),
					WithinCityExtraHoursCharges = Math.Round((pm.WithinCityExtraHoursCharges ?? 0) * (double)rate, 2),
					OutsideCityExtraHoursCharges = Math.Round((pm.OutsideCityExtraHoursCharges ?? 0) * (double)rate, 2),
					WithoutFuelWithinExtraHoursCharges = Math.Round((pm.WithoutFuelWithinExtraHoursCharges ?? 0) * (double)rate, 2),
					WithoutFuelOutsideExtraHoursCharges = Math.Round((pm.WithoutFuelOutsideExtraHoursCharges ?? 0) * (double)rate, 2),
					pm.CreatedBy,
					pm.CreatedByName,
					pm.CreatedOn,
					pm.StatusFlag,
					CurrencyInfo = new
					{
						FromCurrency = p.OriginalCurrency,
						ToCurrency = toCurrency,
						ConversionRate = rate
					}
				};
			}).ToList();

			// Response with paginated data
			var response = new
			{
				CurrentPageNumber = currentPageNumber,
				PageSize = pageSize,
				TotalItems = totalItems,
				TotalPages = totalPages,
				Items = convertedPriceMasters
			};

			return Ok(response);
		}
		[HttpGet("{id}")]
        public async Task <IActionResult> GetPriceMasterById(int id, string toCurrency = "USD")
		{
			double conversionRate = (double)await _currencyService.GetConversionRateAsync("USD", toCurrency);
			var priceMaster = _db.PriceMaster.FirstOrDefault(p => p.PriceMasterId == id);

            if (priceMaster == null)
            {
                return Ok(new { message = "PriceMaster not found." });
            }

            return Ok(priceMaster);
        }

        /*[HttpPost]
        public IActionResult SavePriceMaster([FromBody] PriceMaster priceMaster)
        {
            if (priceMaster == null)
            {
                return BadRequest("Invalid price master data.");
            }

            try
            {
                _db.PriceMaster.Add(priceMaster);
                _db.SaveChanges();

                return Ok(new { message = "PriceMaster saved successfully!", priceMasterId = priceMaster.PriceMasterId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }*/
        [HttpPost]
        public async Task<IActionResult> SavePriceMaster([FromBody] PriceMaster priceMaster)
		{
			

			if (priceMaster == null)
            {
                return BadRequest("Invalid price master data.");
            }

            try
            {
               
                // Check if a PriceMaster already exists for the given VehicleId
                var existingPriceMaster = _db.PriceMaster.FirstOrDefault(p => p.VehicleId == priceMaster.VehicleId);

                if (existingPriceMaster != null)
                {
                    // Update the existing PriceMaster
                    existingPriceMaster.VehicleCategory = !string.IsNullOrEmpty(priceMaster.VehicleCategory)
                        ? priceMaster.VehicleCategory
                        : existingPriceMaster.VehicleCategory;
                    existingPriceMaster.WithinCity = priceMaster.WithinCity;
                    existingPriceMaster.OutsideCity = priceMaster.OutsideCity;
                    existingPriceMaster.WeekDiscount = priceMaster.WeekDiscount;
                    existingPriceMaster.MonthDiscount = priceMaster.MonthDiscount;
                    existingPriceMaster.AirportDay = priceMaster.AirportDay;
                    existingPriceMaster.AirportNight = priceMaster.AirportNight;
                    existingPriceMaster.WithoutFuelWithinCity = priceMaster.WithoutFuelWithinCity;
                    existingPriceMaster.WithoutFuelOutsideCity = priceMaster.WithoutFuelOutsideCity;
                    existingPriceMaster.WithoutFuelWeekDiscount = priceMaster.WithoutFuelWeekDiscount;
                    existingPriceMaster.WithoutFuelMonthDiscount = priceMaster.WithoutFuelMonthDiscount;
                    existingPriceMaster.WithoutFuelAirportDay = priceMaster.WithoutFuelAirportDay;
                    existingPriceMaster.WithoutFuelAirportNight = priceMaster.WithoutFuelAirportNight;
					existingPriceMaster.WithinCityMinimum = priceMaster.WithinCityMinimum ;
					existingPriceMaster.OutsideCityMinimum = priceMaster.OutsideCityMinimum ;
					existingPriceMaster.WithoutFuelWithinCityMinimum = priceMaster.WithoutFuelWithinCityMinimum ;
					existingPriceMaster.WithoutFuelOutsideCityMinimum = priceMaster.WithoutFuelOutsideCityMinimum ;
					existingPriceMaster.WithinCityExtraHoursCharges = priceMaster.WithinCityExtraHoursCharges ;
					existingPriceMaster.OutsideCityExtraHoursCharges = priceMaster.OutsideCityExtraHoursCharges ;
					existingPriceMaster.WithoutFuelWithinExtraHoursCharges = priceMaster.WithoutFuelWithinExtraHoursCharges ;
					existingPriceMaster.WithoutFuelOutsideExtraHoursCharges = priceMaster.WithoutFuelOutsideExtraHoursCharges;
					existingPriceMaster.CreatedBy = (int)this.UserID/*(int)this.UserID*/;
                    existingPriceMaster.CreatedByName = "CreatedByName"/*this.UserEmail*/;

                    existingPriceMaster.StatusFlag = priceMaster.StatusFlag;

                    _db.PriceMaster.Update(existingPriceMaster);
                    _db.SaveChanges();

                    return Ok(new { message = "PriceMaster updated successfully!", priceMasterId = existingPriceMaster.PriceMasterId });
                }
                else
                {
                    // Create a new PriceMaster
                    _db.PriceMaster.Add(priceMaster);
                    _db.SaveChanges();

                    return Ok(new { message = "PriceMaster created successfully!", priceMasterId = priceMaster.PriceMasterId });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpPost("update/{id}")]
        public async Task <IActionResult> UpdatePriceMaster(int id, [FromBody] PriceMaster updatedPriceMaster)
		{
			

			if (updatedPriceMaster == null || id != updatedPriceMaster.PriceMasterId)
            {
                return BadRequest("Invalid price master data or ID mismatch.");
            }

            var existingPriceMaster = _db.PriceMaster.FirstOrDefault(p => p.PriceMasterId == id);

            if (existingPriceMaster == null)
            {
                return Ok(new { message = "PriceMaster not found." });
            }

            try
            {
                // Update properties
                existingPriceMaster.VehicleId = updatedPriceMaster.VehicleId != 0 ? updatedPriceMaster.VehicleId : existingPriceMaster.VehicleId;
                existingPriceMaster.VehicleCategory = !string.IsNullOrEmpty(updatedPriceMaster.VehicleCategory) ? updatedPriceMaster.VehicleCategory : existingPriceMaster.VehicleCategory;
                existingPriceMaster.WithinCity = updatedPriceMaster.WithinCity;
                existingPriceMaster.OutsideCity = updatedPriceMaster.OutsideCity;
                existingPriceMaster.WeekDiscount = updatedPriceMaster.WeekDiscount;
                existingPriceMaster.MonthDiscount = updatedPriceMaster.MonthDiscount;
                existingPriceMaster.AirportDay = updatedPriceMaster.AirportDay;
                existingPriceMaster.AirportNight = updatedPriceMaster.AirportNight;
                existingPriceMaster.WithoutFuelWithinCity = updatedPriceMaster.WithoutFuelWithinCity;
                existingPriceMaster.WithoutFuelOutsideCity = updatedPriceMaster.WithoutFuelOutsideCity;
                existingPriceMaster.WithoutFuelWeekDiscount = updatedPriceMaster.WithoutFuelWeekDiscount;
                existingPriceMaster.WithoutFuelMonthDiscount = updatedPriceMaster.WithoutFuelMonthDiscount;
                existingPriceMaster.WithoutFuelAirportDay = updatedPriceMaster.WithoutFuelAirportDay;
                existingPriceMaster.WithoutFuelAirportNight = updatedPriceMaster.WithoutFuelAirportNight;
				existingPriceMaster.WithinCityMinimum = updatedPriceMaster.WithinCityMinimum;
				existingPriceMaster.OutsideCityMinimum = updatedPriceMaster.OutsideCityMinimum;
				existingPriceMaster.WithoutFuelWithinCityMinimum = updatedPriceMaster.WithoutFuelWithinCityMinimum;
				existingPriceMaster.WithoutFuelOutsideCityMinimum = updatedPriceMaster.WithoutFuelOutsideCityMinimum;
				existingPriceMaster.WithinCityExtraHoursCharges = updatedPriceMaster.WithinCityExtraHoursCharges;
				existingPriceMaster.OutsideCityExtraHoursCharges = updatedPriceMaster.OutsideCityExtraHoursCharges;
				existingPriceMaster.WithoutFuelWithinExtraHoursCharges = updatedPriceMaster.WithoutFuelWithinExtraHoursCharges;
				existingPriceMaster.WithoutFuelOutsideExtraHoursCharges = updatedPriceMaster.WithoutFuelOutsideExtraHoursCharges;
				existingPriceMaster.CreatedBy = (int)this.UserID;
                existingPriceMaster.CreatedByName = this.UserEmail;
                existingPriceMaster.StatusFlag = updatedPriceMaster.StatusFlag;

                _db.PriceMaster.Update(existingPriceMaster);
                _db.SaveChanges();

                return Ok(new { message = "PriceMaster updated successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("delete/{id}")]
        public IActionResult DeletePriceMaster(int id)
        {
            var priceMaster = _db.PriceMaster.FirstOrDefault(p => p.PriceMasterId == id);

            if (priceMaster == null)
            {
                return Ok(new { message = "PriceMaster not found." });
            }

            try
            {
                _db.PriceMaster.Remove(priceMaster);
                _db.SaveChanges();

                return Ok(new { message = "PriceMaster deleted successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
