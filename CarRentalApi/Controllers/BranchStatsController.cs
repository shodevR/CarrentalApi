using CarRentalApi.Data;
using CarRentalApi.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace CarRentalApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BranchStatsController: BaseController
    {
		
		private readonly ApplicationDbContext _db;
		private readonly ICurrencyConversionService _currencyService;

		public BranchStatsController(ApplicationDbContext db
		  , IHttpContextAccessor contextAccessor, IConfiguration configuration, IWebHostEnvironment hostingEnvironment, ICurrencyConversionService currencyService)
			  : base(hostingEnvironment, contextAccessor, configuration, db)
		{
			_db = db;
			_currencyService = currencyService;
			

		}

		[HttpGet("GetBranchStats")]
        public IActionResult GetBranchStats([FromQuery] string branchIds, DateTime? startDate = null, DateTime? endDate = null)
        {
            // Parse the branchIds parameter into an array of integers
            var selectedBranchIds = branchIds?.Split(',')
                                              .Select(id => int.TryParse(id, out var branchId) ? branchId : 0)
                                              .Where(branchId => branchId > 0)
                                              .ToArray();

            if (selectedBranchIds == null || !selectedBranchIds.Any())
            {
                return BadRequest("Please provide valid branch IDs.");
            }

            // Set default date range if not provided
            var effectiveStartDate = startDate ?? DateTime.MinValue;
            var effectiveEndDate = endDate ?? DateTime.MaxValue;

            // Query Drivers
            var driverQuery = _db.Driver.Where(d => selectedBranchIds.Contains(d.BranchId) && d.StatusFlag == true);
            var availableDriversQuery = driverQuery.Where(d => d.LiveStatus == "Available").ToList();

            var bookedDriverIds = _db.Booking
                .Where(b => selectedBranchIds.Contains(b.BranchId) &&
                            b.BookingDateFrom <= effectiveEndDate &&
                            b.BookingDateTo >= effectiveStartDate)
                .Select(b => b.DriverId)
                .Distinct()
                .ToList();

            var availableDriverCount = availableDriversQuery.Count(d => !bookedDriverIds.Contains(d.DriverId));

            // Query Vehicles
            var vehicleQuery = _db.Vehicle.Where(v => selectedBranchIds.Contains(v.BranchId) && v.StatusFlag==true);
            var availableVehiclesQuery = vehicleQuery.Where(v => v.LiveStatus == "Available" || v.LiveStatus == "").ToList();

            var bookedVehicleIds = _db.Booking
                .Where(b => selectedBranchIds.Contains(b.BranchId) &&
                            b.BookingDateFrom <= effectiveEndDate &&
                            b.BookingDateTo >= effectiveStartDate)
                .Select(b => b.VehicleId)
                .Distinct()
                .ToList();

            var availableVehicleCount = availableVehiclesQuery.Count(v => !bookedVehicleIds.Contains(v.VehicleId));

            // Total Counts
            var totalDrivers = driverQuery.Count();
            var totalVehicles = vehicleQuery.Count();
            var totalBookings = _db.Booking
                .Where(b => selectedBranchIds.Contains(b.BranchId) &&
                            b.BookingDateFrom <= effectiveEndDate &&
                            b.BookingDateTo >= effectiveStartDate)
                .Count();

            // Count of Drivers and Vehicles on Trip (LiveStatus == "OnTrip")
            var onTripDriverCount = driverQuery.Count(d => d.LiveStatus == "OnTrip");
            var onTripVehicleCount = vehicleQuery.Count(v => v.LiveStatus == "OnTrip");

            // Count of Vehicles under Maintenance (LiveStatus == "Under Maintenance")
            var underMaintenanceVehicleCount = vehicleQuery.Count(v => v.LiveStatus == "Under Maintenance");

            // Count of documents about to expire
            var currentDate = DateTime.Now;
            var expirationThreshold = currentDate.AddDays(20);

            // Total Expiring Vehicle Documents
            var totalExpiringVehicleDocuments = _db.Document
                .Where(doc => doc.ExpireDate.HasValue &&
                              doc.ExpireDate.Value <= expirationThreshold &&
                              _db.Vehicle.Any(v => v.VehicleId == doc.VehicleId && selectedBranchIds.Contains(v.BranchId)))
                .Count();

            // Total Expiring Driver Documents
            var totalExpiringDriverDocuments = _db.DriverDocuments
                .Where(doc => doc.ExpireDate.HasValue &&
                              doc.ExpireDate.Value <= expirationThreshold &&
                              _db.Driver.Any(d => d.DriverId == doc.DriverId && selectedBranchIds.Contains(d.BranchId)))
                .Count();

            var firstbooking = _context.Booking
			.Where(b => selectedBranchIds.Contains(b.BranchId) && b.BookingDateFrom != null && b.StatusFlag == true)
			.OrderBy(b => b.BookingDateFrom)
			.Select(b => b.BookingDateFrom)
			.FirstOrDefaultAsync();
            // Response
            var response = new
            {
                BranchIds = selectedBranchIds,
                TotalAvailableDrivers = availableDriverCount,
                TotalAvailableVehicles = availableVehicleCount,
                TotalDrivers = totalDrivers,
                TotalVehicles = totalVehicles,
                TotalBookings = totalBookings,
                TotalOnTripDrivers = onTripDriverCount,
                TotalOnTripVehicles = onTripVehicleCount,
                TotalUnderMaintenanceVehicles = underMaintenanceVehicleCount,
                ExpiringDriverDocuments = totalExpiringDriverDocuments,
                ExpiringVehicleDocumentsByBranch = totalExpiringVehicleDocuments,
                FirstbookingDate = firstbooking.Result
            };

            return Ok(response); 
        }
		[HttpGet("GetBranchBookingStats")]
		public async Task<IActionResult> GetBranchBookingStats(
	[FromQuery] string branchIds = "",
	[FromQuery] DateTime? startDate = null,
	[FromQuery] DateTime? endDate = null,
	[FromQuery] string toCurrency = "USD")
		{
			// Parse the branchIds parameter into an array of integers
			var selectedBranchIds = branchIds?.Split(',')
				.Select(id => int.TryParse(id, out var branchId) ? branchId : 0)
				.Where(branchId => branchId > 0)
				.ToArray();

			if (selectedBranchIds == null || !selectedBranchIds.Any())
			{
				return BadRequest("Please provide valid branch IDs.");
			}

			// Set the effective start and end dates
			var effectiveStartDate = startDate ?? DateTime.MinValue;
			var effectiveEndDate = endDate ?? DateTime.MaxValue;

			// Get branch currencies for fallback
			var branchCurrencies = await _db.LocationMaster
				.Where(b => selectedBranchIds.Contains(b.Id))
				.Select(b => new { b.Id, Currency = b.CurrencyCode ?? "USD" })
				.ToDictionaryAsync(b => b.Id, b => b.Currency);

			// Get total vehicles per branch
			var vehicleStats = await _db.Vehicle
				.Where(v => selectedBranchIds.Contains(v.BranchId) && v.StatusFlag == true)
				.GroupBy(v => v.BranchId)
				.Select(g => new
				{
					BranchId = g.Key,
					TotalVehicles = g.Count()
				})
				.ToListAsync();

			// Get total drivers per branch
			var driverStats = await _db.Driver
				.Where(d => selectedBranchIds.Contains(d.BranchId))
				.GroupBy(d => d.BranchId)
				.Select(g => new
				{
					BranchId = g.Key,
					TotalDrivers = g.Count()
				})
				.ToListAsync();

			// Query bookings for the selected branches within the date range with currency info
			var bookings = await _db.Booking
				.Where(b => selectedBranchIds.Contains(b.BranchId) &&
						  b.BookingDateFrom <= effectiveEndDate &&
						  b.BookingDateTo >= effectiveStartDate)
				.Select(b => new
				{
					b.BranchId,
					b.Amount,
					b.CurrencyCode,
					b.ROI,
					b.BookingDateFrom,
					b.BookingDateTo
				})
				.ToListAsync();

			// Group bookings by BranchId and calculate stats with currency conversion
			var bookingStats = bookings
				.GroupBy(b => b.BranchId)
				.Select(g =>
				{
					var branchId = g.Key;
					var branchCurrency = branchCurrencies.GetValueOrDefault(branchId, "USD");

					decimal totalRevenueOriginal = 0;
					decimal totalRevenueConverted = 0;

					foreach (var booking in g)
					{
						decimal amount = booking.Amount ?? 0;
						totalRevenueOriginal += amount;

						// Get conversion rate for this booking
						decimal rate = 1.0m;

						// First try to use booking's currency and ROI
						if (!string.IsNullOrEmpty(booking.CurrencyCode) && booking.ROI.HasValue && booking.ROI.Value > 0)
						{
							if (booking.CurrencyCode == toCurrency)
							{
								rate = 1.0m;
							}
							else
							{
								// ROI is the rate for 1 USD to local currency, so we need to invert it
								// to get local currency to USD
								rate = 1.0m / booking.ROI.Value;
							}
						}
						else
						{
							// Fallback to branch currency
							if (branchCurrency != toCurrency)
							{
								try
								{
									rate = (decimal)_currencyService.GetConversionRateAsync(branchCurrency, toCurrency).Result;
								}
								catch
								{
									rate = 1.0m;
								}
							}
						}

						totalRevenueConverted += amount * rate;
					}

					return new
					{
						BranchId = branchId,
						TotalBookings = g.Count(),
						TotalRevenueOriginal = totalRevenueOriginal,
						TotalRevenueConverted = totalRevenueConverted,
						TotalBookingDays = g.Sum(b => (b.BookingDateTo - b.BookingDateFrom).Days + 1)
					};
				})
				.ToList();

			// Combine the stats for each branch
			var result = selectedBranchIds.Select(branchId =>
			{
				var bookingStat = bookingStats.FirstOrDefault(b => b.BranchId == branchId);
				var branchCurrency = branchCurrencies.GetValueOrDefault(branchId, "USD");

				return new
				{
					BranchId = branchId,
					BranchCurrency = branchCurrency,
					TargetCurrency = toCurrency,
					TotalVehicles = vehicleStats.FirstOrDefault(v => v.BranchId == branchId)?.TotalVehicles ?? 0,
					TotalDrivers = driverStats.FirstOrDefault(d => d.BranchId == branchId)?.TotalDrivers ?? 0,
					TotalBookings = bookingStat?.TotalBookings ?? 0,
					TotalRevenue = bookingStat?.TotalRevenueConverted ?? 0,
					OriginalRevenue = bookingStat?.TotalRevenueOriginal ?? 0,
					TotalBookingDays = bookingStat?.TotalBookingDays ?? 0
				};
			});

			return Ok(result);
		}
		[HttpGet("GetTotalVehiclesByBranchIds")]
		public async Task<IActionResult> GetTotalVehiclesByBranchIds(string? branchIds)
		{
			try
			{
				// Parse branchIds into a list of ints (if not null/empty)
				List<int> branchIdList = new List<int>();
				if (!string.IsNullOrWhiteSpace(branchIds))
				{
					branchIdList = branchIds
						.Split(',', StringSplitOptions.RemoveEmptyEntries)
						.Select(id => int.Parse(id.Trim()))
						.ToList();
				}

				// Query vehicles
				var query = _context.Vehicle.AsQueryable();

				// Only active vehicles
				query = query.Where(v => v.StatusFlag);

				// Apply branch filter if provided
				if (branchIdList.Any())
				{
					query = query.Where(v => branchIdList.Contains(v.BranchId));
				}

				// Group by branch and get counts
				var result = await query
					.GroupBy(v => v.BranchId)
					.Select(g => new
					{
						BranchId = g.Key,
						TotalVehicles = g.Count()
					})
					.ToListAsync();

				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { Message = "Error retrieving vehicle counts", Details = ex.Message });
			}
		}
		[HttpGet("GetCountriesWithBranches")]
		public async Task<IActionResult> GetCountriesWithBranches(string? branchIds)
		{
			try
			{
				// Parse branchIds into a list of ints
				List<int> branchIdList = new List<int>();
				if (!string.IsNullOrWhiteSpace(branchIds))
				{
					branchIdList = branchIds
						.Split(',', StringSplitOptions.RemoveEmptyEntries)
						.Select(id => int.Parse(id.Trim()))
						.ToList();
				}

				// Query BranchMaster (filter if branchIds provided)
				var branchQuery = _context.LocationMaster.AsQueryable();
				if (branchIdList.Any())
				{
					branchQuery = branchQuery.Where(b => branchIdList.Contains(b.Id));
				}

				// Join with CountryMaster and get distinct countries
				var result = await (from country in _context.TP_CountryMaster
									join branch in branchQuery
										on country.ID equals branch.Country
									group country by new { country.ID, country.Name } into g
									select new
									{
										CountryId = g.Key.ID,
										CountryName = g.Key.Name
									})
								   .ToListAsync();

				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { Message = "Error retrieving countries", Details = ex.Message });
			}
		}
		[HttpGet("GetBranchesByCountryIds")]
		public async Task<IActionResult> GetBranchesByCountryIds(string? countryIds)
		{
			try
			{
				// Parse countryIds into a list of ints
				List<int> countryIdList = new List<int>();
				if (!string.IsNullOrWhiteSpace(countryIds))
				{
					countryIdList = countryIds
						.Split(',', StringSplitOptions.RemoveEmptyEntries)
						.Select(id => int.Parse(id.Trim()))
						.ToList();
				}

				// Query BranchMaster
				var branchQuery = _context.LocationMaster.AsQueryable();

				if (countryIdList.Any())
				{
					branchQuery = branchQuery.Where(b => countryIdList.Contains(b.Country));
				}

				var result = await branchQuery
					.Select(b => new
					{
						BranchId = b.Id,
						BranchName = b.LocationName,
						CountryId = b.Country
					})
					.ToListAsync();

				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { Message = "Error retrieving branches", Details = ex.Message });
			}
		}



	}
}
