using CarRentalApi.Data;
using CarRentalApi.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System;
using CarRentalApi.Service;
using Microsoft.EntityFrameworkCore;

namespace CarRentalApi.Controllers                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehicleMaintenanceController :BaseController
    {
		
		private readonly ApplicationDbContext _db;
		private readonly ICurrencyConversionService _currencyService;

		public VehicleMaintenanceController(ApplicationDbContext db
		  , IHttpContextAccessor contextAccessor, IConfiguration configuration, IWebHostEnvironment hostingEnvironment, ICurrencyConversionService currencyService)
			  : base(hostingEnvironment, contextAccessor, configuration, db)
		{
			_db = db;
			_currencyService = currencyService;
			

		}

		[HttpPost("start")]
        public IActionResult StartMaintenance([FromBody] VehicleMaintenance maintenance)
        {
            if (maintenance == null)
            {
                return BadRequest("Invalid maintenance data.");
            }

            try
            {
                // Ensure mandatory fields for starting maintenance are provided
                if (maintenance.VehicleId == 0 || maintenance.StartDate == DateTime.MinValue || string.IsNullOrEmpty(maintenance.MaintenanceIssuedBy))
                {
                    return BadRequest("VehicleId, StartDate, and MaintenanceIssuedBy are required.");
                }

                // Add new maintenance record
                _db.VehicleMaintenance.Add(new VehicleMaintenance
                {
                    VehicleId = maintenance.VehicleId,
                    BranchId = maintenance.BranchId,
                    StartDate = maintenance.StartDate,
                    ExpectedReturnDate = maintenance.ExpectedReturnDate,
                    Reason = maintenance.Reason,
                    GarageName = maintenance.GarageName,
                    KmIn = maintenance.KmIn,
                    DriverId= maintenance.DriverId,
                    MaintenanceIssuedBy = maintenance.MaintenanceIssuedBy,
                    CreatedBy = (int)this.UserID,
                    CreatedByName = this.UserEmail,
                StatusFlag = true // Maintenance is ongoing
                });
                _db.SaveChanges();

                // Update the vehicle status to "Under Maintenance"
                var vehicle = _db.Vehicle.FirstOrDefault(v => v.VehicleId == maintenance.VehicleId);
                if (vehicle != null)
                {
                    vehicle.LiveStatus = "Under Maintenance";
                    _db.Vehicle.Update(vehicle);
                    _db.SaveChanges();
                }

                return Ok(new { message = "Maintenance started successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("complete/{id}")]
		public async Task<IActionResult> CompleteMaintenance(int id, [FromBody] VehicleMaintenance updatedMaintenance)
        {
			
			
            var existingMaintenance = _db.VehicleMaintenance.FirstOrDefault(vm => vm.VehicleMaintenanceId == id);

            if (existingMaintenance == null)
            {
                return Ok(new { message = "Maintenance record not found." });
            }

            try
            {
                // Update maintenance completion details
                existingMaintenance.ReturnDate = updatedMaintenance.ReturnDate != DateTime.MinValue ? updatedMaintenance.ReturnDate : DateTime.Now;
                existingMaintenance.KmOut = updatedMaintenance.KmOut;
                existingMaintenance.Cost = updatedMaintenance.Cost;
                existingMaintenance.Description = updatedMaintenance.Description;
                existingMaintenance.NextDate = updatedMaintenance.NextDate != DateTime.MinValue ? updatedMaintenance.NextDate : DateTime.Now.AddMonths(6);
                existingMaintenance.CreatedBy = updatedMaintenance.CreatedBy;
                existingMaintenance.CreatedByName = updatedMaintenance.CreatedByName;
                existingMaintenance.StatusFlag = false; // Maintenance completed

                _db.VehicleMaintenance.Update(existingMaintenance);
                _db.SaveChanges();

                // Revert the vehicle's status to "Available"
                var vehicle = _db.Vehicle.FirstOrDefault(v => v.VehicleId == existingMaintenance.VehicleId);
                if (vehicle != null)
                {
                    vehicle.LiveStatus = "Available";
                    _db.Vehicle.Update(vehicle);
                    _db.SaveChanges();
                }

                return Ok(new { message = "Maintenance completed successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


		/*  [HttpGet]
          public IActionResult GetVehicleMaintenances([FromQuery] string searchText = "", int branchId = 0, int vehicleId = 0, int currentPageNumber = 1, int pageSize = 50)
          {
              var query = _db.VehicleMaintenance.AsQueryable();

              // Filter by BranchId
              if (branchId > 0)
              {
                  query = query.Where(vm => vm.BranchId == branchId);
              }

              // Filter by VehicleId
              if (vehicleId > 0)
              {
                  query = query.Where(vm => vm.VehicleId == vehicleId);
              }

              // Search functionality (Example: search by GarageName or Reason)
              if (!string.IsNullOrEmpty(searchText))
              {
                  query = query.Where(vm => vm.GarageName.Contains(searchText) || vm.Reason.Contains(searchText));
              }

              // Pagination
              int totalItems = query.Count();
              int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
              var vehicleMaintenances = query.Skip((currentPageNumber - 1) * pageSize).Take(pageSize).ToList();

              var response = new PagedResponse<VehicleMaintenance>
              {
                  CurrentPageNumber = currentPageNumber,
                  PageSize = pageSize,
                  TotalItems = totalItems,
                  TotalPages = totalPages,
                  Items = vehicleMaintenances
              };

              return Ok(response);
          }*/
		[HttpGet]
		public async Task<IActionResult> GetVehicleMaintenances(
	 [FromQuery] string searchText = "",
	 [FromQuery] string branchIds = "",
	 [FromQuery] int vehicleId = 0,
	 [FromQuery] int currentPageNumber = 1,
	 [FromQuery] int pageSize = 50,
	 [FromQuery] string toCurrency = "USD")
		{
			// Join VehicleMaintenance with Vehicle and Branch
			var query = from vm in _db.VehicleMaintenance
						join v in _db.Vehicle on vm.VehicleId equals v.VehicleId
						join b in _db.LocationMaster on vm.BranchId equals b.Id into branchJoin
						from b in branchJoin.DefaultIfEmpty()
						select new
						{
							Maintenance = vm,
							Vehicle = v,
							Branch = b,
							OriginalCurrency = b != null ? b.CurrencyCode ?? "USD" : "USD"
						};

			// Apply filters
			if (!string.IsNullOrEmpty(branchIds))
			{
				var branchIdList = branchIds.Split(',')
										   .Select(int.Parse)
										   .ToList();
				query = query.Where(x => branchIdList.Contains(x.Maintenance.BranchId));
			}

			if (vehicleId > 0)
			{
				query = query.Where(x => x.Maintenance.VehicleId == vehicleId);
			}

			if (!string.IsNullOrEmpty(searchText))
			{
				query = query.Where(x => x.Maintenance.GarageName.Contains(searchText) ||
										x.Maintenance.Reason.Contains(searchText));
			}

			// Get distinct currencies from the results
			var currencies = await query
				.Select(x => x.OriginalCurrency)
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

			// Order by VehicleMaintenanceId in descending order
			query = query.OrderByDescending(x => x.Maintenance.VehicleMaintenanceId);

			// Pagination
			int totalItems = await query.CountAsync();
			int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
			var vehicleMaintenances = await query
				.Skip((currentPageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			// Convert costs to target currency
			var convertedMaintenances = vehicleMaintenances.Select(x =>
			{
				var vm = x.Maintenance;
				var rate = x.OriginalCurrency == toCurrency
					? 1.0m
					: conversionRates.GetValueOrDefault(x.OriginalCurrency, 1.0m);

				return new
				{
					vm.VehicleMaintenanceId,
					vm.BranchId,
					vm.VehicleId,
					vm.StartDate,
					vm.ReturnDate,
					vm.Reason,
					vm.ExpectedReturnDate,
					vm.GarageName,
					vm.KmIn,
					vm.KmOut,
					Cost = vm.Cost * rate,
					OriginalCost = vm.Cost,
					vm.Description,
					vm.NextDate,
					vm.MaintenanceIssuedBy,
					vm.DriverId,
					vm.StatusFlag,
					x.Vehicle.VehicleName,
					vm.CreatedBy,
					vm.CreatedByName,
					x.Vehicle.VehicleNumber,
					CurrencyInfo = new
					{
						FromCurrency = x.OriginalCurrency,
						ToCurrency = toCurrency,
						ConversionRate = rate
					}
				};
			}).ToList();

			var response = new
			{
				CurrentPageNumber = currentPageNumber,
				PageSize = pageSize,
				TotalItems = totalItems,
				TotalPages = totalPages,
				Items = convertedMaintenances
			};

			return Ok(response);
		}

		[HttpGet("{id}")]
        public IActionResult GetVehicleMaintenanceById(int id)
        {
            var vehicleMaintenance = _db.VehicleMaintenance.FirstOrDefault(vm => vm.VehicleMaintenanceId == id);

            if (vehicleMaintenance == null)
            {
                return Ok(new { message = "VehicleMaintenance record not found." });
            }

            return Ok(vehicleMaintenance);
        }

        [HttpPost]
        public async Task <IActionResult> SaveVehicleMaintenance([FromBody] VehicleMaintenance vehicleMaintenance)
		{

			

			if (vehicleMaintenance == null)
            {
                return BadRequest("Invalid vehicle maintenance data.");
            }

            try
            {
                vehicleMaintenance.CreatedBy = (int)this.UserID;
                vehicleMaintenance.CreatedByName = this.UserEmail;
                _db.VehicleMaintenance.Add(vehicleMaintenance);
                _db.SaveChanges();

                // Update the vehicle status to "Under Maintenance"
                var vehicle = _db.Vehicle.FirstOrDefault(v => v.VehicleId == vehicleMaintenance.VehicleId);
                if (vehicle != null)
                {
                    vehicle.LiveStatus = "Under Maintenance";
                    _db.Vehicle.Update(vehicle);
                    _db.SaveChanges();
                }

                return Ok(new { message = "VehicleMaintenance record saved successfully!", maintenanceId = vehicleMaintenance.VehicleMaintenanceId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("update/{id}")]
        public async Task<IActionResult> UpdateVehicleMaintenance(int id, [FromBody] VehicleMaintenance updatedMaintenance)
		{

			
			if (updatedMaintenance == null || updatedMaintenance.VehicleMaintenanceId != id)
            {
                return BadRequest("Invalid vehicle maintenance data or ID mismatch.");
            }

            var existingMaintenance = _db.VehicleMaintenance.FirstOrDefault(vm => vm.VehicleMaintenanceId == id);

            if (existingMaintenance == null)
            {
                return Ok("VehicleMaintenance record not found.");
            }

            try
            {
                // Update properties only if they are provided
                existingMaintenance.BranchId = updatedMaintenance.BranchId != 0 ? updatedMaintenance.BranchId : existingMaintenance.BranchId;
                existingMaintenance.StartDate = updatedMaintenance.StartDate != DateTime.MinValue ? updatedMaintenance.StartDate : existingMaintenance.StartDate;
                existingMaintenance.Cost = updatedMaintenance.Cost != 0 ? updatedMaintenance.Cost : existingMaintenance.Cost;
                existingMaintenance.NextDate = updatedMaintenance.NextDate != DateTime.MinValue ? updatedMaintenance.NextDate : existingMaintenance.NextDate;
                existingMaintenance.ReturnDate = updatedMaintenance.ReturnDate != DateTime.MinValue ? updatedMaintenance.ReturnDate : existingMaintenance.ReturnDate;
                existingMaintenance.ExpectedReturnDate = updatedMaintenance.ExpectedReturnDate != DateTime.MinValue ? updatedMaintenance.ExpectedReturnDate : existingMaintenance.ExpectedReturnDate;
                existingMaintenance.Reason = !string.IsNullOrEmpty(updatedMaintenance.Reason) ? updatedMaintenance.Reason : existingMaintenance.Reason;
                existingMaintenance.Description = !string.IsNullOrEmpty(updatedMaintenance.Description) ? updatedMaintenance.Description : existingMaintenance.Description;
                existingMaintenance.GarageName = !string.IsNullOrEmpty(updatedMaintenance.GarageName) ? updatedMaintenance.GarageName : existingMaintenance.GarageName;
                existingMaintenance.VehicleId = updatedMaintenance.VehicleId != 0 ? updatedMaintenance.VehicleId : existingMaintenance.VehicleId;
                existingMaintenance.KmIn = updatedMaintenance.KmIn != 0 ? updatedMaintenance.KmIn : existingMaintenance.KmIn;
                existingMaintenance.KmOut = updatedMaintenance.KmOut != 0 ? updatedMaintenance.KmOut : existingMaintenance.KmOut;
                existingMaintenance.MaintenanceIssuedBy = updatedMaintenance.MaintenanceIssuedBy != default ? updatedMaintenance.MaintenanceIssuedBy : existingMaintenance.MaintenanceIssuedBy;
                existingMaintenance.DriverId = updatedMaintenance.DriverId != 0 ? updatedMaintenance.DriverId : existingMaintenance.DriverId;
                existingMaintenance.CreatedBy = (int)this.UserID;
                existingMaintenance.CreatedByName = "updatedByName";
                existingMaintenance.StatusFlag = updatedMaintenance.StatusFlag;

                _db.VehicleMaintenance.Update(existingMaintenance);
                _db.SaveChanges();

                // Update the vehicle status based on the maintenance date range
                var vehicle = _db.Vehicle.FirstOrDefault(v => v.VehicleId == existingMaintenance.VehicleId);
                if (vehicle != null)
                {
                    // If the current date is within the maintenance date range, set the vehicle to "Under Maintenance"
                    if (DateTime.Now >= existingMaintenance.StartDate && DateTime.Now <= existingMaintenance.ReturnDate)
                    {
                        vehicle.LiveStatus = "Under Maintenance";
                    }
                    else
                    {
                        vehicle.LiveStatus = "Available"; // Revert status to available after maintenance period ends
                    }

                    _db.Vehicle.Update(vehicle);
                    _db.SaveChanges();
                }

                return Ok(new { message = "VehicleMaintenance record updated successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("delete/{id}")]
        public IActionResult DeleteVehicleMaintenance(int id)
        {
            var vehicleMaintenance = _db.VehicleMaintenance.FirstOrDefault(vm => vm.VehicleMaintenanceId == id);

            if (vehicleMaintenance == null)
            {
                return Ok(new { message = "VehicleMaintenance record not found." });
            }

            try
            {

                vehicleMaintenance.StatusFlag = false;
                _db.VehicleMaintenance.Update(vehicleMaintenance);
                _db.SaveChanges();

                // Revert the vehicle's status to "Available" after maintenance is deleted
                var vehicle = _db.Vehicle.FirstOrDefault(v => v.VehicleId == vehicleMaintenance.VehicleId);
                if (vehicle != null)
                {
                    vehicle.LiveStatus = "Available";
                    _db.Vehicle.Update(vehicle);
                    _db.SaveChanges();
                }

                return Ok(new { message = "VehicleMaintenance record deleted successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
