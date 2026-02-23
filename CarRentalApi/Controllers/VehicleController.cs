using CarRentalApi.Data;
using CarRentalApi.Model;
using CarRentalApi.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CarRentalApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehicleController :BaseController
    {
		
		private readonly ApplicationDbContext _db;
        private readonly IImageUploadService _imageUploadService;
        private readonly VehicleService _vehicleService;
		private readonly ICurrencyConversionService _currencyService;

		public VehicleController(ApplicationDbContext db, IImageUploadService imageUploadService, VehicleService vehicleService, IHttpContextAccessor contextAccessor, IConfiguration configuration, IWebHostEnvironment hostingEnvironment, ICurrencyConversionService currencyService) : base(hostingEnvironment, contextAccessor, configuration, db)
        {
            _db = db;
            _imageUploadService = imageUploadService;
            _vehicleService = vehicleService;
			_currencyService = currencyService;
			
		}


      

        [HttpGet("expiring-documents")]
        public async Task<IActionResult> GetExpiringDocuments(int vehicleId, DateTime dateFrom, DateTime dateTo)
        {
            var messages = await _vehicleService.GetExpiringDocumentsAsync(vehicleId, dateFrom, dateTo);

            if (messages.Count == 0)
            {
                return Ok("No documents are expiring within the given range or next 7 days of the booking range.");
            }

            return Ok(messages);
        }
        /*[HttpGet]
        public IActionResult GetVehicles([FromQuery] string searchText = "", int branchId = 0, int currentPageNumber = 1, int pageSize = 50, int orderByColNum = 1)
        {
            var query = _db.Vehicle.AsQueryable();

            // Filter by BranchId
            if (branchId > 0)
            {
                query = query.Where(v => v.BranchId == branchId);
            }

            // Search functionality
            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(v => v.VehicleName.Contains(searchText)
                                      || v.VehicleNumber.Contains(searchText)
                                      || v.VINNo.Contains(searchText)
                                      || v.Model.Contains(searchText)
                                      || v.Company.Contains(searchText));
            }

            // Order by functionality
            switch (orderByColNum)
            {
                case 1:
                    query = query.OrderBy(v => v.VehicleName);
                    break;
                case 2:
                    query = query.OrderByDescending(v => v.ManufacturingYear);
                    break;
                case 3:
                    query = query.OrderByDescending(v => v.TotalKm);
                    break;
                case 4:
                    query = query.OrderBy(v => v.RegistrationExpire);
                    break;
                default:
                    query = query.OrderBy(v => v.VehicleName);
                    break;
            }

            // Pagination
            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var vehicles = query.Skip((currentPageNumber - 1) * pageSize).Take(pageSize).ToList();

            var response = new PagedResponse<Vehicle>
            {
                CurrentPageNumber = currentPageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                BranchId = branchId,  // Set the branchId in the response
                Items = vehicles
            };

            return Ok(response);
        }*/
        [HttpGet]
		public async Task<IActionResult> GetVehicles(
	[FromQuery] string searchText = "",
    [FromQuery] string branchIds = "", // Change to accept comma-separated string
    int currentPageNumber = 1,
    int pageSize = 50,
    int orderByColNum = 1,
    string toCurrency = "USD")
        {
            double conversionRate = (double)await _currencyService.GetConversionRateAsync("USD", toCurrency);
           
			var query = _db.Vehicle.AsQueryable()/*.Where(v=>v.StatusFlag == true)*/;

            // Filter by BranchId
            if (!string.IsNullOrEmpty(branchIds))
            {
                // Parse comma-separated string into a list of integers
                var branchIdList = branchIds.Split(',')
                                            .Select(id => int.Parse(id))
                                            .ToList();

                // Filter by the list of branch IDs
                query = query.Where(v => branchIdList.Contains(v.BranchId));
            }

            // Search functionality
            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(v => v.VehicleName.Contains(searchText)
                                          || v.VehicleNumber.Contains(searchText)
                                          || v.VINNo.Contains(searchText)
                                          || v.Model.Contains(searchText)
                                          || v.Company.Contains(searchText));
            }

            // Order by functionality
            switch (orderByColNum)
            {
                case 1:
                    query = query.OrderBy(v => v.VehicleName);
                    break;
                case 2:
                    query = query.OrderByDescending(v => v.ManufacturingYear);
                    break;
                case 3:
                    query = query.OrderByDescending(v => v.TotalKm);
                    break;
                case 4:
                    query = query.OrderBy(v => v.RegistrationExpire);
                    break;
                default:
                    query = query.OrderBy(v => v.VehicleName);
                    break;
            }

            // Always order by VehicleId in descending order at the end
            query = query.OrderByDescending(v => v.VehicleId);

            // Include pricing from PriceMaster and the most recent service date from maintenance
            var vehiclesWithPricing = query
                .Select(v => new
                {
                    Vehicle = v,
                    LastMaintenanceDate = _db.VehicleMaintenance
                        .Where(vm => vm.VehicleId == v.VehicleId )
                        .OrderByDescending(vm => vm.StartDate)
                        .Select(vm => vm.StartDate)
                        .FirstOrDefault(),
                    Pricing = _db.PriceMaster
                        .Where(pm => pm.VehicleId == v.VehicleId)
                        .Select(pm => new
                        {
							WithinCity = pm.WithinCity * conversionRate,
							OutsideCity = pm.OutsideCity * conversionRate,
                            WeekDiscount = pm.WeekDiscount,
                            MonthDiscount = pm.MonthDiscount,
                            AirportDay = pm.AirportDay * conversionRate,
                            AirportNight = pm.AirportNight * conversionRate,
                            WithoutFuelWithinCity= pm.WithoutFuelWithinCity * conversionRate,
							WithoutFuelOutsideCity =pm.WithoutFuelOutsideCity * conversionRate,
                            WithoutFuelWeekDiscount=pm.WithoutFuelWeekDiscount,
                            WithoutFuelMonthDiscount=pm.WithoutFuelMonthDiscount,
                            WithoutFuelAirportDay=pm.WithoutFuelAirportDay * conversionRate,
                            WithoutFuelAirportNight=pm.WithoutFuelAirportNight * conversionRate
						})
                        .FirstOrDefault()
                })
                .ToList()
                .Select(vwp => new
                {
                    vwp.Vehicle.VehicleId,
                    vwp.Vehicle.VehicleName,
                    vwp.Vehicle.Model,
                    vwp.Vehicle.VehicleType,
                    vwp.Vehicle.MainPhoto,
                    vwp.Vehicle.VehicleNumber,
                    vwp.Vehicle.ChasisNumber,
                    vwp.Vehicle.ManufacturingYear,
                    vwp.Vehicle.RegistrationDate,
                    vwp.Vehicle.Company,
                    vwp.Vehicle.LiveStatus,
                    vwp.Vehicle.Fuel,
                    vwp.Vehicle.BranchId,
                    vwp.Vehicle.RegistrationExpire,
                    vwp.Vehicle.VINNo,
                    vwp.Vehicle.InsuranceNo,
                    LastServiceDate = vwp.LastMaintenanceDate != default(DateTime)
                        ? vwp.LastMaintenanceDate.ToString("yyyy-MM-dd")
                        : vwp.Vehicle.LastServiceDate,
                    vwp.Vehicle.Features,
                    vwp.Vehicle.TotalTrips,
                    vwp.Vehicle.TotalKm,
                    vwp.Vehicle.Milage,
                    vwp.Vehicle.CreatedBy,
                    vwp.Vehicle.CreatedByName,
                    vwp.Vehicle.StatusFlag,
                    WithinCity = vwp.Pricing?.WithinCity ?? 0,
                    OutsideCity = vwp.Pricing?.OutsideCity ?? 0,
                    WeekDiscount = vwp.Pricing?.WeekDiscount ?? 0,
                    MonthDiscount = vwp.Pricing?.MonthDiscount ?? 0,
                    AirportDay = vwp.Pricing?.AirportDay ?? 0,
                    AirportNight = vwp.Pricing?.AirportNight ?? 0,
                    WithoutFuelWithinCity = vwp.Pricing?.WithoutFuelWithinCity ?? 0,
                    WithoutFuelOutsideCity = vwp.Pricing?.WithoutFuelOutsideCity ?? 0,
                    WithoutFuelWeekDiscount = vwp.Pricing?.WithoutFuelWeekDiscount ?? 0,
                    WithoutFuelMonthDiscount = vwp.Pricing?.WithoutFuelMonthDiscount ?? 0,
                    WithoutFuelAirportDay = vwp.Pricing?.WithoutFuelAirportDay ?? 0,
                    WithoutFuelAirportNight = vwp.Pricing?.WithoutFuelAirportNight ?? 0

                })
                .ToList();

            // Pagination
            int totalItems = vehiclesWithPricing.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var paginatedVehicles = vehiclesWithPricing.Skip((currentPageNumber - 1) * pageSize).Take(pageSize).ToList();

            var response = new
            {
                CurrentPageNumber = currentPageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                BranchId = branchIds,
                Items = paginatedVehicles
            };

            return Ok(response);
        }



        [HttpPost("/savenewMethod")]
        public async Task<IActionResult> SaveVehicle1([FromForm] Vehicle vehicle, IFormFile imageFile)
        {
            if (vehicle == null)
            {
                return Ok("Invalid vehicle data.");
            }

            try
            {
                if (imageFile != null)
                {
                    // Upload the image using the reusable service
                    vehicle.MainPhoto = await _imageUploadService.UploadImageAsync(imageFile);
                }
                // Add the new vehicle to the database
                _db.Vehicle.Add(vehicle);
                _db.SaveChanges();

                return Ok(new { message = "Vehicle saved successfully!", vehicleId = vehicle.VehicleId });
            }
            catch (Exception ex)
            {
                return Ok($"Internal server error: {ex.Message}");
            }
        }
        [HttpPost]
        public IActionResult SaveVehicle([FromBody] Vehicle vehicle)
        {
            if (vehicle == null)
            {
                return BadRequest("Invalid vehicle data.");
            }

            try
            {
                vehicle.LiveStatus = "Available";
                vehicle.CreatedBy= (int)this.UserID;
                vehicle.CreatedByName = this.UserEmail;
                // Add the new vehicle to the database
                _db.Vehicle.Add(vehicle);
                _db.SaveChanges();

                return Ok(new { message = "Vehicle saved successfully!", vehicleId = vehicle.VehicleId });
            }
            catch (Exception ex)
            {
                return Ok($"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("update/{id}")]
        public IActionResult UpdateVehicle(int id, [FromBody] Vehicle updatedVehicle)
        {
            if (updatedVehicle == null || id != updatedVehicle.VehicleId)
            {
                return Ok("Invalid vehicle data or ID mismatch.");
            }

            var existingVehicle = _db.Vehicle.FirstOrDefault(v => v.VehicleId == id);

            if (existingVehicle == null)
            {
                return Ok("Vehicle not found.");
            }

            try
            {
                // Update properties only if they are provided
                existingVehicle.VehicleName = !string.IsNullOrEmpty(updatedVehicle.VehicleName) ? updatedVehicle.VehicleName : existingVehicle.VehicleName;
                existingVehicle.Model = !string.IsNullOrEmpty(updatedVehicle.Model) ? updatedVehicle.Model : existingVehicle.Model;
                existingVehicle.VehicleType = !string.IsNullOrEmpty(updatedVehicle.VehicleType) ? updatedVehicle.VehicleType : existingVehicle.VehicleType;
                existingVehicle.MainPhoto = !string.IsNullOrEmpty(updatedVehicle.MainPhoto) ? updatedVehicle.MainPhoto : existingVehicle.MainPhoto;
                existingVehicle.VehicleNumber = !string.IsNullOrEmpty(updatedVehicle.VehicleNumber) ? updatedVehicle.VehicleNumber : existingVehicle.VehicleNumber;
                existingVehicle.ChasisNumber = !string.IsNullOrEmpty(updatedVehicle.ChasisNumber) ? updatedVehicle.ChasisNumber : existingVehicle.ChasisNumber;
                existingVehicle.ManufacturingYear = updatedVehicle.ManufacturingYear != 0 ? updatedVehicle.ManufacturingYear : existingVehicle.ManufacturingYear;
                existingVehicle.RegistrationDate = !string.IsNullOrEmpty(updatedVehicle.RegistrationDate) ? updatedVehicle.RegistrationDate : existingVehicle.RegistrationDate;
                existingVehicle.Company = !string.IsNullOrEmpty(updatedVehicle.Company) ? updatedVehicle.Company : existingVehicle.Company;
                existingVehicle.Fuel = !string.IsNullOrEmpty(updatedVehicle.Fuel) ? updatedVehicle.Fuel : existingVehicle.Fuel;
                existingVehicle.BranchId = updatedVehicle.BranchId != 0 ? updatedVehicle.BranchId : existingVehicle.BranchId;
                existingVehicle.RegistrationExpire = !string.IsNullOrEmpty(updatedVehicle.RegistrationExpire) ? updatedVehicle.RegistrationExpire : existingVehicle.RegistrationExpire;
                existingVehicle.LiveStatus = !string.IsNullOrEmpty(updatedVehicle.LiveStatus) ? updatedVehicle.LiveStatus : existingVehicle.LiveStatus;
                existingVehicle.VINNo = !string.IsNullOrEmpty(updatedVehicle.VINNo) ? updatedVehicle.VINNo : existingVehicle.VINNo;
                existingVehicle.InsuranceNo = !string.IsNullOrEmpty(updatedVehicle.InsuranceNo) ? updatedVehicle.InsuranceNo : existingVehicle.InsuranceNo;
                existingVehicle.LastServiceDate = !string.IsNullOrEmpty(updatedVehicle.LastServiceDate) ? updatedVehicle.LastServiceDate : existingVehicle.LastServiceDate;
                existingVehicle.Features = !string.IsNullOrEmpty(updatedVehicle.Features) ? updatedVehicle.Features : existingVehicle.Features;
                existingVehicle.TotalTrips = updatedVehicle.TotalTrips != 0 ? updatedVehicle.TotalTrips : existingVehicle.TotalTrips;
                existingVehicle.TotalKm = updatedVehicle.TotalKm != 0 ? updatedVehicle.TotalKm : existingVehicle.TotalKm;
                existingVehicle.Milage = updatedVehicle.Milage != 0 ? updatedVehicle.Milage : existingVehicle.Milage;
                existingVehicle.CreatedBy =existingVehicle.CreatedBy;
                existingVehicle.CreatedByName = existingVehicle.CreatedByName;

                _db.Vehicle.Update(existingVehicle);
                _db.SaveChanges();

                return Ok(new { message = "Vehicle updated successfully!" });
            }
            catch (Exception ex)
            {
                return Ok($"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task <IActionResult> GetVehicleById(int id, string toCurrency = "USD")
		{
			double conversionRate = (double)await _currencyService.GetConversionRateAsync("USD", toCurrency);
			var vehicleData = _db.Vehicle
        .Where(v => v.VehicleId == id)
        .Select(v => new
        {
            Vehicle = v,
            LastMaintenanceDate = _db.VehicleMaintenance
                .Where(vm => vm.VehicleId == v.VehicleId)
                .OrderByDescending(vm => vm.StartDate)
                .Select(vm => vm.StartDate)
                .FirstOrDefault(),
            Pricing = _db.PriceMaster
                .Where(pm => pm.VehicleId == v.VehicleId)
                .Select(pm => new
                {
					WithinCity = pm.WithinCity * conversionRate,
					OutsideCity = pm.OutsideCity * conversionRate,
					WeekDiscount = pm.WeekDiscount,
					MonthDiscount = pm.MonthDiscount,
					AirportDay = pm.AirportDay * conversionRate,
					AirportNight = pm.AirportNight * conversionRate,
					WithoutFuelWithinCity = pm.WithoutFuelWithinCity * conversionRate,
					WithoutFuelOutsideCity = pm.WithoutFuelOutsideCity * conversionRate,
					WithoutFuelWeekDiscount = pm.WithoutFuelWeekDiscount,
					WithoutFuelMonthDiscount = pm.WithoutFuelMonthDiscount,
					WithoutFuelAirportDay = pm.WithoutFuelAirportDay * conversionRate,
					WithoutFuelAirportNight = pm.WithoutFuelAirportNight * conversionRate
				})
                .FirstOrDefault()
        })
        .FirstOrDefault();

            if (vehicleData == null)
            {
                return Ok($"Vehicle with ID {id} not found.");
            }

            var result = new
            {
                vehicleData.Vehicle.VehicleId,
                vehicleData.Vehicle.VehicleName,
                vehicleData.Vehicle.Model,
                vehicleData.Vehicle.VehicleType,
                vehicleData.Vehicle.MainPhoto,
                vehicleData.Vehicle.VehicleNumber,
                vehicleData.Vehicle.ChasisNumber,
                vehicleData.Vehicle.ManufacturingYear,
                vehicleData.Vehicle.RegistrationDate,
                vehicleData.Vehicle.Company,
                vehicleData.Vehicle.LiveStatus,
                vehicleData.Vehicle.Fuel,
                vehicleData.Vehicle.BranchId,
                vehicleData.Vehicle.RegistrationExpire,
                vehicleData.Vehicle.VINNo,
                vehicleData.Vehicle.InsuranceNo,
                LastServiceDate = vehicleData.LastMaintenanceDate != default(DateTime)
                    ? vehicleData.LastMaintenanceDate.ToString("yyyy-MM-dd")
                    : vehicleData.Vehicle.LastServiceDate,
                vehicleData.Vehicle.Features,
                vehicleData.Vehicle.TotalTrips,
                vehicleData.Vehicle.TotalKm,
                vehicleData.Vehicle.Milage,
                vehicleData.Vehicle.StatusFlag,
                vehicleData.Vehicle.CreatedBy,
                vehicleData.Vehicle.CreatedByName,
                WithinCity = vehicleData.Pricing?.WithinCity ?? 0,
                OutsideCity = vehicleData.Pricing?.OutsideCity ?? 0,
                WeekDiscount = vehicleData.Pricing?.WeekDiscount ?? 0,
                MonthDiscount = vehicleData.Pricing?.MonthDiscount ?? 0,
                AirportDay = vehicleData.Pricing?.AirportDay ?? 0,
                AirportNight = vehicleData.Pricing?.AirportNight ?? 0
            };

            return Ok(result);
        }

		/* [HttpGet("available")]
         public IActionResult GetAvailableVehicles(
     [FromQuery] DateTime startDate,
     DateTime endDate,
     int branchId = 0,
     bool isAirportDay = false,
     bool isAirportNight = false,
     bool isWithinCity = true,
     bool isWithoutFuel = false) // New parameter
         {
             var query = _db.Vehicle.AsQueryable();

             // Filter by BranchId
             if (branchId > 0)
             {
                 query = query.Where(v => v.BranchId == branchId);
             }
             query = query.Where(v => v.StatusFlag == true);

             // Get all booked vehicle IDs
             var bookedVehicleIds = _db.Booking
                 .Where(b => (b.BookingDateFrom <= endDate && b.BookingDateTo >= startDate && b.StatusFlag == true))
                 .Select(b => b.VehicleId)
                 .Distinct()
                 .ToList();

             var maintenanceIds = _db.VehicleMaintenance
                 .Where(b => (b.StartDate <= endDate && b.ReturnDate >= startDate))
                 .Select(b => b.VehicleId)
                 .Distinct()
                 .ToList();

             // Exclude booked and under-maintenance vehicles
             query = query.Where(v => !bookedVehicleIds.Contains(v.VehicleId) && !maintenanceIds.Contains(v.VehicleId));

             // Calculate total days
             int totalDays = (endDate - startDate).Days + 1;
             if ((isAirportDay || isAirportNight) && totalDays != 1)
             {
                 return Ok("Airport options can only be selected on the Same Day.");
             }

             // Join with PriceMaster to fetch pricing
             var availableVehicles = query
                 .GroupJoin(
                     _db.PriceMaster,
                     v => v.VehicleId,
                     pm => pm.VehicleId,
                     (v, pm) => new { Vehicle = v, Pricing = pm.FirstOrDefault() }
                 )
                 .AsEnumerable()
                 .Select(result =>
                 {
                     var pricing = result.Pricing;

                     // Determine pricing based on fuel option
                     double cityRate = isWithoutFuel
                         ? pricing?.WithoutFuelWithinCity ?? 0.0
                         : pricing?.WithinCity ?? 0.0;
                     double outsideRate = isWithoutFuel
                         ? pricing?.WithoutFuelOutsideCity ?? 0.0
                         : pricing?.OutsideCity ?? 0.0;

                     decimal? weekDiscount = isWithoutFuel
                         ? pricing?.WithoutFuelWeekDiscount
                         : pricing?.WeekDiscount;
                     decimal? monthDiscount = isWithoutFuel
                         ? pricing?.WithoutFuelMonthDiscount
                         : pricing?.MonthDiscount;

                     // Calculate total amount
                     var (totalAmount, weekDiscountOrMonthly) = CalculateTotalAmount(
                         totalDays,
                         isWithinCity ? cityRate : outsideRate,
                         weekDiscount,
                         monthDiscount,
                        isAirportDay ? pricing?.AirportDay ?? 0.0 : 0.0,
             isAirportNight ? pricing?.AirportNight ?? 0.0 : 0.0,
                         startDate,
                         endDate,
                         isAirportDay,
                         isAirportNight
                     );

                     if (isAirportDay)
                     {
                         totalAmount = pricing?.AirportDay ?? 0.0;
                     }
                     else if (isAirportNight)
                     {
                         totalAmount = pricing?.AirportNight ?? 0.0;
                     }

                     return new
                     {
                         result.Vehicle.VehicleId,
                         result.Vehicle.VehicleName,
                         result.Vehicle.Model,
                         result.Vehicle.VehicleType,
                         result.Vehicle.MainPhoto,
                         result.Vehicle.VehicleNumber,
                         result.Vehicle.ChasisNumber,
                         result.Vehicle.Company,
                         result.Vehicle.ManufacturingYear,
                         result.Vehicle.BranchId,
                         result.Vehicle.LiveStatus,
                         result.Vehicle.Fuel,
                         result.Vehicle.InsuranceNo,
                         result.Vehicle.RegistrationExpire,
                         result.Vehicle.TotalKm,
                         result.Vehicle.Milage,
                         result.Vehicle.Features,
                         result.Vehicle.CreatedBy,
                         result.Vehicle.CreatedByName,
                         result.Vehicle.StatusFlag,
                         TotalAmount = totalAmount,
                         WeekDiscountOrMonthly = weekDiscountOrMonthly,
                         totalDays
                     };
                 })
                 .OrderByDescending(v => v.VehicleId)
                 .ToList();

             return Ok(availableVehicles);
         }
 */
		[HttpGet("available")]
		public async Task<IActionResult> GetAvailableVehicles(
	 [FromQuery] DateTime startDate,
	 [FromQuery] DateTime endDate,
	 [FromQuery] int branchId = 0,
	 [FromQuery] bool isAirportDay = false,
	 [FromQuery] bool isAirportNight = false,
	 [FromQuery] bool isWithinCity = true,
	 [FromQuery] bool isWithoutFuel = false,
	 [FromQuery] string toCurrency = "USD")
		{
			var query = _db.Vehicle.AsQueryable();

			// Filter by BranchId
			if (branchId > 0)
			{
				query = query.Where(v => v.BranchId == branchId);
			}
			query = query.Where(v => v.StatusFlag == true);

			// Get all bookings in date range
			var overlappingBookings = await _db.Booking
				.Where(b => b.StatusFlag == true &&
							b.BookingDateFrom <= endDate &&
							b.BookingDateTo >= startDate)
				.Select(b => new { b.BookingId, b.VehicleId })
				.ToListAsync();

			// Get all bookings with completed checklist
			var completedChecklistBookingIds = await _db.CheckList
				.Where(cl => cl.OdometerAfter > 0)
				.Select(cl => cl.BookingId)
				.Distinct()
				.ToListAsync();

			// Exclude only bookings in the range where the checklist is NOT complete
			var bookedVehicleIds = overlappingBookings
				.Where(b => !completedChecklistBookingIds.Contains(b.BookingId))
				.Select(b => b.VehicleId)
				.Distinct()
				.ToList();

			var maintenanceIds = await _db.VehicleMaintenance
				.Where(b => b.StartDate <= endDate && b.ReturnDate >= startDate)
				.Select(b => b.VehicleId)
				.Distinct()
				.ToListAsync();

			// Exclude booked and under-maintenance vehicles
			query = query.Where(v => !bookedVehicleIds.Contains(v.VehicleId) && !maintenanceIds.Contains(v.VehicleId));

			// Calculate total days
			int totalDays = (endDate - startDate).Days + 1;
			if ((isAirportDay || isAirportNight) && totalDays != 1)
			{
				return BadRequest("Airport options can only be selected on the Same Day.");
			}

			// NEW: Get branch currencies for available vehicles
			var branchIdsInResults = await query.Select(v => v.BranchId).Distinct().ToListAsync();
			var branchCurrencies = await _db.LocationMaster
				.Where(b => branchIdsInResults.Contains(b.Id))
				.Select(b => new { b.Id, Currency = b.CurrencyCode ?? "USD" })
				.ToDictionaryAsync(b => b.Id, b => b.Currency);

			// NEW: Get conversion rates for unique currencies
			var uniqueCurrencies = branchCurrencies.Values
				.Distinct()
				.Where(c => c != toCurrency)
				.ToList();

			var conversionRates = new Dictionary<string, double>();
			foreach (var currency in uniqueCurrencies)
			{
				conversionRates[currency] = (double)await _currencyService.GetConversionRateAsync(currency, toCurrency);
			}

			// Join with PriceMaster to fetch pricing
			var availableVehicles = await query
				.GroupJoin(
					_db.PriceMaster,
					v => v.VehicleId,
					pm => pm.VehicleId,
					(v, pm) => new { Vehicle = v, Pricing = pm.FirstOrDefault() }
				)
				.ToListAsync();

			var result = availableVehicles.Select(result =>
			{
				var vehicle = result.Vehicle;
				var pricing = result.Pricing;

				// Get conversion rate for this vehicle's branch
				var rate = branchCurrencies.TryGetValue(vehicle.BranchId, out var fromCurrency)
					? (fromCurrency == toCurrency ? 1.0 : conversionRates.GetValueOrDefault(fromCurrency, 1.0))
					: 1.0;

				// Determine pricing based on fuel option
				double cityRate = isWithoutFuel
					? pricing?.WithoutFuelWithinCity ?? 0.0
					: pricing?.WithinCity ?? 0.0;
				double outsideRate = isWithoutFuel
					? pricing?.WithoutFuelOutsideCity ?? 0.0
					: pricing?.OutsideCity ?? 0.0;

				decimal? weekDiscount = isWithoutFuel
					? pricing?.WithoutFuelWeekDiscount
					: pricing?.WeekDiscount;
				decimal? monthDiscount = isWithoutFuel
					? pricing?.WithoutFuelMonthDiscount
					: pricing?.MonthDiscount;

				double MinimumRateWithin = isWithoutFuel
					? pricing?.WithoutFuelWithinCityMinimum ?? 0.0
					: pricing?.WithinCityMinimum ?? 0.0;
				double MinimumRateOutside = isWithoutFuel
					? pricing?.WithoutFuelOutsideCityMinimum ?? 0.0
					: pricing?.OutsideCityMinimum ?? 0.0;

				// Calculate total amount
				var (totalAmount, weekDiscountOrMonthly) = CalculateTotalAmount(
					totalDays,
					isWithinCity ? cityRate : outsideRate,
					weekDiscount,
					monthDiscount,
					isAirportDay ? pricing?.AirportDay ?? 0.0 : 0.0,
					isAirportNight ? pricing?.AirportNight ?? 0.0 : 0.0,
					startDate,
					endDate,
					isAirportDay,
					isAirportNight
				);

				if (isAirportDay)
				{
					totalAmount = pricing?.AirportDay ?? 0.0;
				}
				else if (isAirportNight)
				{
					totalAmount = pricing?.AirportNight ?? 0.0;
				}

				return new
				{
					vehicle.VehicleId,
					vehicle.VehicleName,
					vehicle.Model,
					vehicle.VehicleType,
					vehicle.MainPhoto,
					vehicle.VehicleNumber,
					vehicle.ChasisNumber,
					vehicle.Company,
					vehicle.ManufacturingYear,
					vehicle.BranchId,
					vehicle.LiveStatus,
					vehicle.Fuel,
					vehicle.InsuranceNo,
					vehicle.RegistrationExpire,
					vehicle.TotalKm,
					vehicle.Milage,
					vehicle.Features,
					vehicle.CreatedBy,
					vehicle.CreatedByName,
					vehicle.StatusFlag,
					TotalAmount = totalAmount * rate,
					OriginalTotalAmount = totalAmount,
					WeekDiscountOrMonthly = weekDiscountOrMonthly,
					TotalDays = totalDays,
					MinimumRateWithin = MinimumRateWithin * rate,
					MinimumRateOutside = MinimumRateOutside * rate,
					CurrencyInfo = new
					{
						FromCurrency = branchCurrencies.GetValueOrDefault(vehicle.BranchId, "USD"),
						ToCurrency = toCurrency,
						ConversionRate = rate
					}
				};
			})
			.OrderByDescending(v => v.VehicleId)
			.ToList();

			return Ok(result);
		}
		private static (double TotalAmount, decimal? WeekDiscountOrMonthly) CalculateTotalAmount(
     int totalDays,
     double rate, // Generalized rate (WithinCity or OutsideCity)
     decimal? weekDiscount,
     decimal? monthDiscount,
     double airportDayRate,
     double airportNightRate,
     DateTime startDate,
     DateTime endDate,
     bool isAirportDay,
     bool isAirportNight)
        {
            double baseAmount = totalDays * rate;

            decimal? weekDiscountOrMonthly = null;

            // Determine if the date range spans the full month or overlaps months
            bool isFullMonth = startDate.Day == 1 && endDate.Month == startDate.Month && endDate.AddDays(1).Month != startDate.Month;
            bool isSpanningMonths = ((startDate.Day > 1 || endDate.Day <= startDate.AddMonths(1).AddDays(-1).Day) && totalDays >= 28);

            // Apply discounts based on conditions

            if (totalDays >= 7 && totalDays < 28 && weekDiscount.HasValue)
            {
                weekDiscountOrMonthly = weekDiscount;

            }
            else if ((isFullMonth || isSpanningMonths) && monthDiscount.HasValue && totalDays >= 28 && startDate.Month != endDate.AddDays(1).Month)
            {
                weekDiscountOrMonthly = monthDiscount;

            }
            else if (weekDiscount.HasValue && totalDays >= 7)
            {
                weekDiscountOrMonthly = weekDiscount;


            }
            else
            {
                weekDiscountOrMonthly = 0;
            }




            double airportCharge = airportDayRate + airportNightRate;

            double totalAmount = baseAmount + airportCharge;
            return (totalAmount, weekDiscountOrMonthly);
        }


        /* private static double CalculateTotalAmount(
             int totalDays,
             double rate, // Generalized rate (WithinCity or OutsideCity)
             decimal? weekDiscount,
             decimal? monthDiscount,
             double airportDayRate,
             double airportNightRate)
         {
             double baseAmount = totalDays * rate;
             double discount = 0;

             // Apply discounts based on total days
             if (totalDays >= 7 && totalDays < 30 && weekDiscount.HasValue)
             {
                 discount = baseAmount * (double)weekDiscount.Value / 100;
             }
             else if (totalDays >= 30 && monthDiscount.HasValue)
             {
                 discount = baseAmount * (double)monthDiscount.Value / 100;
             }

             // Add airport charges if applicable
             double airportCharge = airportDayRate + airportNightRate;

             return baseAmount - discount + airportCharge;
         }*/

        [HttpPost("delete/{id}")]
        public IActionResult DeleteVehicle(int id)
        {
            var vehicle = _db.Vehicle.FirstOrDefault(v => v.VehicleId == id);

            if (vehicle == null)
            {
                return Ok(new { message = "Vehicle not found." });
            }

            try
            {
                vehicle.StatusFlag = false;

                // Remove the vehicle from the database
                _db.Vehicle.Update(vehicle);
                _db.SaveChanges();

                return Ok(new { message = "Vehicle deleted successfully!" });
            }
            catch (Exception ex)
            {
                return Ok($"Internal server error: {ex.Message}");
            }
        }
        [HttpGet("GetVehiclesByType")]
        public IActionResult GetVehiclesByType([FromQuery] string vehicleType, [FromQuery] int? branchId = null)
        {
            if (string.IsNullOrWhiteSpace(vehicleType))
            {
                return Ok("VehicleType is required.");
            }

            var vehicles = _db.Vehicle
                             .Where(v => v.VehicleType == vehicleType &&
                                         (!branchId.HasValue || v.BranchId == branchId.Value) && v.StatusFlag == true) // Filter by BranchId if provided
                             .OrderByDescending(v => v.VehicleId) // Sorting by VehicleId in descending order
                             .ToList();

            if (!vehicles.Any())
            {
                return Ok(new { message = $"No vehicles found with VehicleType: {vehicleType}" });
            }

            return Ok(vehicles);
        }

        // GET: api/Vehicle/GetVehiclesByType?vehicleType=Sedan
        [HttpGet("GetVehiclesByTypeNew")]
        public async Task <IActionResult> GetFilteredVehicles(
            [FromQuery] int? branchId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? vehicleType,
			 string toCurrency = "USD")
		{
			double conversionRate = (double)await _currencyService.GetConversionRateAsync("USD", toCurrency);
			// Fetch all vehicles
			var query = _db.Vehicle.Where(v => v.StatusFlag).AsQueryable(); // Only include vehicles with StatusFlag == true
            
            // Filter by BranchId
            if (branchId.HasValue && branchId > 0)
            {
                query = query.Where(v => v.BranchId == branchId);
            }

            // Filter by vehicle type if provided and not "All"
            if (!string.IsNullOrWhiteSpace(vehicleType) && !vehicleType.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(v => v.VehicleType == vehicleType);
            }

            // Get all vehicle IDs that are booked within the given date range
            var bookedVehicleIds = _db.Booking
    .Where(b => ((b.BookingDateFrom <= endDate && b.BookingDateTo >= startDate) && b.StatusFlag == true))
    .Select(b => b.VehicleId)
    
    .ToList();
            Console.WriteLine("Booked Vehicle IDs: " + string.Join(", ", bookedVehicleIds));




            // Retrieve all vehicles with the above filters and calculate availability
            var vehicleData = query
                .OrderByDescending(v => v.VehicleId) // Sort by VehicleId in descending order
                .Select(v => new
                {
                    v.VehicleId,
                    v.VehicleName,
                    v.Model,
                    v.VehicleType,
                    v.MainPhoto,
                    v.VehicleNumber,
                    v.ChasisNumber,
                    v.ManufacturingYear,
                    v.RegistrationDate,
                    v.Company,
                    v.LiveStatus,
                    v.Fuel,
                    v.BranchId,
                    v.RegistrationExpire,
                    v.VINNo,
                    v.InsuranceNo,
                    v.Features,
                    v.TotalTrips,
                    v.TotalKm,
                    v.Milage,
                    v.StatusFlag,
                    v.CreatedBy,
                    v.CreatedByName,
                    Pricing = _db.PriceMaster
                        .Where(pm => pm.VehicleId == v.VehicleId)
                        .Select(pm => new
                        {
							WithinCity = pm.WithinCity * conversionRate,
							OutsideCity = pm.OutsideCity * conversionRate,
							WeekDiscount = pm.WeekDiscount,
							MonthDiscount = pm.MonthDiscount,
							AirportDay = pm.AirportDay * conversionRate,
							AirportNight = pm.AirportNight * conversionRate,
							WithoutFuelWithinCity = pm.WithoutFuelWithinCity * conversionRate,
							WithoutFuelOutsideCity = pm.WithoutFuelOutsideCity * conversionRate,
							WithoutFuelWeekDiscount = pm.WithoutFuelWeekDiscount,
							WithoutFuelMonthDiscount = pm.WithoutFuelMonthDiscount,
							WithoutFuelAirportDay = pm.WithoutFuelAirportDay * conversionRate,
							WithoutFuelAirportNight = pm.WithoutFuelAirportNight * conversionRate
						})
                        .FirstOrDefault(),
                    // Determine vehicle availability
                    vehicleAvailable = v.LiveStatus == "Available" &&
                   !bookedVehicleIds.Contains(v.VehicleId)
                })
                .ToList();

            // Final result mapping
            var result = vehicleData.Select(v => new
            {
                v.VehicleId,
                v.VehicleName,
                v.Model,
                v.VehicleType,
                v.MainPhoto,
                v.VehicleNumber,
                v.ChasisNumber,
                v.ManufacturingYear,
                v.RegistrationDate,
                v.Company,
                v.LiveStatus,
                v.Fuel,
                v.BranchId,
                v.RegistrationExpire,
                v.VINNo,
                v.InsuranceNo,
                v.Features,
                v.TotalTrips,
                v.TotalKm,
                v.Milage,
                v.StatusFlag,
                v.CreatedBy,
                WithinCity = v.Pricing?.WithinCity ?? 0,
                OutsideCity = v.Pricing?.OutsideCity ?? 0,
                WeekDiscount = v.Pricing?.WeekDiscount ?? 0,
                MonthDiscount = v.Pricing?.MonthDiscount ?? 0,
                AirportDay = v.Pricing?.AirportDay ?? 0,
                AirportNight = v.Pricing?.AirportNight ?? 0,
                vehicleAvailable = v.vehicleAvailable // Keep availability calculation as is
            });

            return Ok(new { Data = result });
        }

		[HttpPost("ChangeStatus/{id}")]
		public IActionResult ChangeStatus(int id)
		{
			var vehicle = _db.Vehicle.FirstOrDefault(d => d.VehicleId == id);

			if (vehicle == null)
			{
				return Ok(new { message = "Vehicle not found." });
			}

			try
			{
				if (vehicle.StatusFlag)
				{
					vehicle.StatusFlag = false;
				}
				else
				{
					vehicle.StatusFlag = true;
				}
				_db.Vehicle.Update(vehicle);
				_db.SaveChanges();
				return Ok(new { message = "Vehicle Status Changed successfully!" });
			}
			catch (Exception ex)
			{
				return Ok($"Internal server error: {ex.Message}");
			}
		}



	}


}
