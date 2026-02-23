using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using CarRentalApi.Data;
using CarRentalApi.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalApi.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ReportController : BaseController
	{
		private readonly ApplicationDbContext _db;
		private readonly ICurrencyConversionService _currencyService;

		public ReportController(ApplicationDbContext db
		  , IHttpContextAccessor contextAccessor, IConfiguration configuration, IWebHostEnvironment hostingEnvironment, ICurrencyConversionService currencyService)
			  : base(hostingEnvironment, contextAccessor, configuration, db)
		{
			_db = db;
			_currencyService = currencyService;
		}

		[HttpGet("VehicleReport")]
		public async Task<IActionResult> GetVehicleReport(
			[FromQuery, Required] string branchIds,
			[FromQuery, Required] DateTime startDate,
			[FromQuery, Required] DateTime endDate,
			[FromQuery] string? sortBy = "Revenue",
			[FromQuery] string? sortOrder = "desc",
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 10,
			[FromQuery] string toCurrency = "USD")
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			if (startDate > endDate)
			{
				return BadRequest("Start date must be before end date.");
			}

			// Parse and validate branch IDs
			List<int> branchIdsList;
			try
			{
				branchIdsList = branchIds.Split(',')
					.Select(int.Parse)
					.Distinct()
					.ToList();
			}
			catch
			{
				ModelState.AddModelError("branchIds", "Invalid branch ID format");
				return BadRequest(ModelState);
			}

			if (!branchIdsList.Any())
			{
				ModelState.AddModelError("branchIds", "At least one branch ID must be provided");
				return BadRequest(ModelState);
			}

			// Get branch currencies for fallback
			var branchCurrencies = await _db.LocationMaster
				.Where(b => branchIdsList.Contains(b.Id))
				.Select(b => new { b.Id, Currency = b.CurrencyCode ?? "USD" })
				.ToDictionaryAsync(b => b.Id, b => b.Currency);

			// Base vehicle query
			var vehiclesQuery = _db.Vehicle
				.Where(v => branchIdsList.Contains(v.BranchId) && v.StatusFlag);

			int totalCount = await vehiclesQuery.CountAsync();

			// Pagination
			var vehicles = (pageSize <= 0)
				? await vehiclesQuery.OrderBy(v => v.VehicleId).ToListAsync()
				: await vehiclesQuery.OrderBy(v => v.VehicleId)
								   .Skip((page - 1) * pageSize)
								   .Take(pageSize)
								   .ToListAsync();

			var vehicleIds = vehicles.Select(v => v.VehicleId).ToList();

			// Get relevant bookings with currency information
			var relevantBookings = await _db.Booking
				.Where(b => vehicleIds.Contains(b.VehicleId) &&
						  b.BookingDateFrom <= endDate &&
						  b.BookingDateTo >= startDate)
				.Select(b => new
				{
					b.VehicleId,
					b.Amount,
					b.DriverId,
					b.WithFuel,
					b.CurrencyCode,
					b.ROI,
					b.BookingDateFrom,
					b.BookingDateTo
				})
				.ToListAsync();

			// Get relevant maintenance records
			var relevantMaintenances = await _db.VehicleMaintenance
				.Where(m => vehicleIds.Contains(m.VehicleId) &&
						  m.StartDate <= endDate &&
						  m.ReturnDate >= startDate)
				.ToListAsync();

			int totalDays = (endDate - startDate).Days + 1;

			// Generate report data with currency conversion
			var vehicleReports = new List<VehicleReportDto>();
			foreach (var v in vehicles)
			{
				var vehicleBookings = relevantBookings
					.Where(b => b.VehicleId == v.VehicleId)
					.ToList();

				var vehicleMaintenances = relevantMaintenances
					.Where(m => m.VehicleId == v.VehicleId && m.StatusFlag == true)
					.ToList();

				// Calculate maintenance days
				var maintenanceIntervals = vehicleMaintenances
					.Select(m => new Interval
					{
						Start = m.StartDate,
						End = m.ReturnDate ?? endDate
					})
					.ToList();

				var mergedMaintenance = MergeIntervals(maintenanceIntervals);
				int maintenanceDays = CalculateCoveredDays(mergedMaintenance, startDate, endDate);

				// Count various booking metrics
				int totalBookings = vehicleBookings.Count;
				int bookingsWithDriver = vehicleBookings.Count(b => b.DriverId.HasValue);
				int bookingsWithoutDriver = vehicleBookings.Count(b => !b.DriverId.HasValue);
				int bookingsWithFuel = vehicleBookings.Count(b => b.WithFuel == true);
				int bookingsWithoutFuel = vehicleBookings.Count(b => b.WithFuel == false);

				// Calculate busy days from bookings
				var bookingIntervals = vehicleBookings
					.Select(b => new Interval { Start = b.BookingDateFrom, End = b.BookingDateTo })
					.OrderBy(i => i.Start)
					.ToList();

				var mergedBookings = MergeIntervals(bookingIntervals);
				int busyDays = CalculateCoveredDays(mergedBookings, startDate, endDate);
				int unusedDays = totalDays - busyDays - maintenanceDays;
				unusedDays = unusedDays < 0 ? 0 : unusedDays; // Ensure not negative
				double bookingRatio = totalDays > 0 ? (double)busyDays / totalDays : 0;

				// Calculate revenue with currency conversion from booking records
				decimal revenue = 0;
				decimal originalRevenue = 0;

				foreach (var booking in vehicleBookings)
				{
					decimal amount = booking.Amount ?? 0;
					originalRevenue += amount;

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
						var branchCurrency1 = branchCurrencies.GetValueOrDefault(v.BranchId, "USD");
						if (branchCurrency1 != toCurrency)
						{
							try
							{
								rate = (decimal)await _currencyService.GetConversionRateAsync(branchCurrency1, toCurrency);
							}
							catch
							{
								rate = 1.0m;
							}
						}
					}

					revenue += amount * rate;
				}

				// Calculate maintenance cost (using branch currency as fallback)
				decimal maintenanceCost = 0;
				decimal originalMaintenanceCost = vehicleMaintenances.Sum(m => m.Cost);

				var branchCurrency = branchCurrencies.GetValueOrDefault(v.BranchId, "USD");
				if (branchCurrency != toCurrency)
				{
					try
					{
						var rate = (decimal)await _currencyService.GetConversionRateAsync(branchCurrency, toCurrency);
						maintenanceCost = originalMaintenanceCost * rate;
					}
					catch
					{
						maintenanceCost = originalMaintenanceCost;
					}
				}
				else
				{
					maintenanceCost = originalMaintenanceCost;
				}

				vehicleReports.Add(new VehicleReportDto
				{
					VehicleId = v.VehicleId,
					VehicleName = v.VehicleName,
					VehicleType = v.VehicleType,
					VehicleNumber = v.VehicleNumber,
					VehicleCompany = v.Company,
					VehicleModel = v.Model,
					BranchId = v.BranchId,
					BranchCurrency = branchCurrencies.GetValueOrDefault(v.BranchId, "USD"),
					TargetCurrency = toCurrency,
					TotalDays = totalDays,
					BusyDays = busyDays,
					UnusedDays = unusedDays,
					Revenue = revenue,
					OriginalRevenue = originalRevenue,
					MaintenanceDays = maintenanceDays,
					MaintenanceCost = maintenanceCost,
					OriginalMaintenanceCost = originalMaintenanceCost,
					BookingRatio = Math.Round(bookingRatio, 2),
					TotalBookings = totalBookings,
					BookingsWithFuel = bookingsWithFuel,
					BookingsWithoutFuel = bookingsWithoutFuel,
					BookingsWithDriver = bookingsWithDriver,
					BookingsWithoutDriver = bookingsWithoutDriver
				});
			}

			// Apply sorting
			vehicleReports = ApplySorting(vehicleReports, sortBy, sortOrder);

			var result = new
			{
				TotalCount = totalCount,
				Page = page,
				PageSize = pageSize,
				TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
				Data = vehicleReports
			};

			return Ok(result);
		}

		[HttpGet("DriverReport")]
		public async Task<IActionResult> GetDriverReport(
			[FromQuery, Required] string branchIds,
			[FromQuery, Required] DateTime startDate,
			[FromQuery, Required] DateTime endDate,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 10,
			[FromQuery] string toCurrency = "USD")
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			if (startDate > endDate)
			{
				return BadRequest("Start date must be before end date.");
			}

			// Parse and validate branch IDs
			List<int> branchIdsList;
			try
			{
				branchIdsList = branchIds.Split(',')
					.Select(int.Parse)
					.Distinct()
					.ToList();
			}
			catch
			{
				ModelState.AddModelError("branchIds", "Invalid branch ID format");
				return BadRequest(ModelState);
			}

			if (!branchIdsList.Any())
			{
				ModelState.AddModelError("branchIds", "At least one branch ID must be provided");
				return BadRequest(ModelState);
			}

			// Get branch currencies for fallback
			var branchCurrencies = await _db.LocationMaster
				.Where(b => branchIdsList.Contains(b.Id))
				.Select(b => new { b.Id, Currency = b.CurrencyCode ?? "USD" })
				.ToDictionaryAsync(b => b.Id, b => b.Currency);

			int totalDays = (endDate - startDate).Days + 1;

			// Get drivers with branch information
			var driversQuery = _db.Driver
				.Where(d => branchIdsList.Contains(d.BranchId) && d.StatusFlag);

			int totalCount = await driversQuery.CountAsync();

			// Pagination
			var drivers = (pageSize <= 0)
				? await driversQuery.OrderBy(d => d.DriverId).ToListAsync()
				: await driversQuery.OrderBy(d => d.DriverId)
								  .Skip((page - 1) * pageSize)
								  .Take(pageSize)
								  .ToListAsync();

			var driverIds = drivers.Select(d => d.DriverId).ToList();

			// Get relevant bookings with currency information
			var relevantBookings = await _db.Booking
				.Where(b => b.DriverId.HasValue &&
						  driverIds.Contains(b.DriverId.Value) &&
						  b.BookingDateFrom <= endDate &&
						  b.BookingDateTo >= startDate)
				.Select(b => new
				{
					b.DriverId,
					b.DriverPrice,
					b.CurrencyCode,
					b.ROI,
					b.BookingDateFrom,
					b.BookingDateTo
				})
				.ToListAsync();

			var relevantLeaves = await _db.DriverLeave
				.Where(l => driverIds.Contains(l.DriverId) &&
						  l.LeaveDateFrom <= endDate &&
						  l.LeaveDateTo >= startDate)
				.ToListAsync();

			var driverReports = new List<DriverReportDto>();
			foreach (var d in drivers)
			{
				var driverBookings = relevantBookings
					.Where(b => b.DriverId == d.DriverId)
					.ToList();

				var driverLeaves = relevantLeaves
					.Where(l => l.DriverId == d.DriverId)
					.ToList();

				// Calculate busy days from bookings
				var busyDays = CalculateCoveredDays(
					MergeIntervals(driverBookings
						.Select(b => new Interval
						{
							Start = b.BookingDateFrom,
							End = b.BookingDateTo
						})),
					startDate,
					endDate
				);

				// Calculate leave days
				var leaveDays = CalculateCoveredDays(
					MergeIntervals(driverLeaves
						.Select(l => new Interval
						{
							Start = l.LeaveDateFrom,
							End = l.LeaveDateTo
						})),
					startDate,
					endDate
				);

				var unusedDays = Math.Max(0, totalDays - (busyDays + leaveDays));

				// Calculate revenue with currency conversion from booking records
				decimal revenue = 0;
				decimal originalRevenue = 0;

				foreach (var booking in driverBookings)
				{
					decimal amount = booking.DriverPrice ?? 0;
					originalRevenue += amount;

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
						var branchCurrency = branchCurrencies.GetValueOrDefault(d.BranchId, "USD");
						if (branchCurrency != toCurrency)
						{
							try
							{
								rate = (decimal)await _currencyService.GetConversionRateAsync(branchCurrency, toCurrency);
							}
							catch
							{
								rate = 1.0m;
							}
						}
					}

					revenue += amount * rate;
				}

				var bookingRatio = totalDays > 0 ? Math.Round((double)busyDays / totalDays, 2) : 0;

				driverReports.Add(new DriverReportDto
				{
					DriverId = d.DriverId,
					DriverName = d.DriverName ?? "Unknown",
					BranchId = d.BranchId,
					BranchCurrency = branchCurrencies.GetValueOrDefault(d.BranchId, "USD"),
					TargetCurrency = toCurrency,
					TotalDays = totalDays,
					BusyDays = busyDays,
					LeaveDays = leaveDays,
					UnusedDays = unusedDays,
					Revenue = revenue,
					OriginalRevenue = originalRevenue,
					BookingRatio = bookingRatio
				});
			}

			var result = new
			{
				TotalCount = totalCount,
				Page = page,
				PageSize = pageSize,
				TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
				Data = driverReports
			};

			return Ok(result);
		}

		// Helper method for sorting
		private static List<VehicleReportDto> ApplySorting(List<VehicleReportDto> reports, string sortBy, string sortOrder)
		{
			return (sortBy?.ToLower(), sortOrder?.ToLower()) switch
			{
				("vehiclename", "asc") => reports.OrderBy(v => v.VehicleName).ToList(),
				("vehiclename", _) => reports.OrderByDescending(v => v.VehicleName).ToList(),
				("vehiclenumber", "asc") => reports.OrderBy(v => v.VehicleNumber).ToList(),
				("vehiclenumber", _) => reports.OrderByDescending(v => v.VehicleNumber).ToList(),
				("vehicletype", "asc") => reports.OrderBy(v => v.VehicleType).ToList(),
				("vehicletype", _) => reports.OrderByDescending(v => v.VehicleType).ToList(),
				("totalbookings", "asc") => reports.OrderBy(v => v.TotalBookings).ToList(),
				("totalbookings", _) => reports.OrderByDescending(v => v.TotalBookings).ToList(),
				("busydays", "asc") => reports.OrderBy(v => v.BusyDays).ToList(),
				("busydays", _) => reports.OrderByDescending(v => v.BusyDays).ToList(),
				("unuseddays", "asc") => reports.OrderBy(v => v.UnusedDays).ToList(),
				("unuseddays", _) => reports.OrderByDescending(v => v.UnusedDays).ToList(),
				("bookingswithdriver", "asc") => reports.OrderBy(v => v.BookingsWithDriver).ToList(),
				("bookingswithdriver", _) => reports.OrderByDescending(v => v.BookingsWithDriver).ToList(),
				("bookingswithoutdriver", "asc") => reports.OrderBy(v => v.BookingsWithoutDriver).ToList(),
				("bookingswithoutdriver", _) => reports.OrderByDescending(v => v.BookingsWithoutDriver).ToList(),
				("bookingswithfuel", "asc") => reports.OrderBy(v => v.BookingsWithFuel).ToList(),
				("bookingswithfuel", _) => reports.OrderByDescending(v => v.BookingsWithFuel).ToList(),
				("bookingswithoutfuel", "asc") => reports.OrderBy(v => v.BookingsWithoutFuel).ToList(),
				("bookingswithoutfuel", _) => reports.OrderByDescending(v => v.BookingsWithoutFuel).ToList(),
				("revenue", "asc") => reports.OrderBy(v => v.Revenue).ToList(),
				("revenue", _) => reports.OrderByDescending(v => v.Revenue).ToList(),
				("maintenancecost", "asc") => reports.OrderBy(v => v.MaintenanceCost).ToList(),
				("maintenancecost", _) => reports.OrderByDescending(v => v.MaintenanceCost).ToList(),
				("maintenancedays", "asc") => reports.OrderBy(v => v.MaintenanceDays).ToList(),
				("maintenancedays", _) => reports.OrderByDescending(v => v.MaintenanceDays).ToList(),
				("bookingratio", "asc") => reports.OrderBy(v => v.BookingRatio).ToList(),
				("bookingratio", _) => reports.OrderByDescending(v => v.BookingRatio).ToList(),
				_ => reports.OrderByDescending(v => v.Revenue).ToList()
			};
		}

		private List<Interval> MergeIntervals(IEnumerable<Interval> intervals)
		{
			var sorted = intervals.OrderBy(i => i.Start).ToList();
			List<Interval> merged = new();

			foreach (var interval in sorted)
			{
				if (!merged.Any())
				{
					merged.Add(interval);
				}
				else
				{
					var last = merged.Last();
					if (interval.Start <= last.End.AddDays(1))
					{
						var newEnd = interval.End > last.End ? interval.End : last.End;
						merged[^1] = new Interval { Start = last.Start, End = newEnd };
					}
					else
					{
						merged.Add(interval);
					}
				}
			}
			return merged;
		}

		private int CalculateCoveredDays(List<Interval> intervals, DateTime reportStart, DateTime reportEnd)
		{
			int days = 0;
			foreach (var interval in intervals)
			{
				var effectiveStart = interval.Start < reportStart ? reportStart : interval.Start;
				var effectiveEnd = interval.End > reportEnd ? reportEnd : interval.End;

				if (effectiveStart > effectiveEnd) continue;

				days += (effectiveEnd - effectiveStart).Days + 1;
			}
			return days;
		}

		[HttpGet]
		public IActionResult GetBookings(
			[FromQuery] string searchText = "",
			[FromQuery] string branchIds = "",
			int clientId = 0,
			int driverId = 0,
			int vehicleId = 0,
			bool? withDriver = null,
			bool? withFuel = null,
			string tripType = "",
			DateTime? startDate = null,
			DateTime? endDate = null,
			int currentPageNumber = 1,
			int pageSize = 50,
			string sortOrder = "desc")
		{
			var query = _db.Booking.AsQueryable();

			// Filter conditions
			if (!string.IsNullOrEmpty(branchIds))
			{
				var branchIdList = branchIds.Split(',').Select(int.Parse).ToList();
				query = query.Where(b => branchIdList.Contains(b.BranchId));
			}
			if (clientId > 0) query = query.Where(b => b.ClientId == clientId);
			if (driverId > 0) query = query.Where(b => b.DriverId == driverId);
			if (vehicleId > 0) query = query.Where(b => b.VehicleId == vehicleId);
			if (startDate.HasValue && endDate.HasValue)
			{
				query = query.Where(b =>
					(b.BookingDateFrom >= startDate.Value && b.BookingDateFrom <= endDate.Value) ||
					(b.BookingDateTo >= startDate.Value && b.BookingDateTo <= endDate.Value) ||
					(b.BookingDateFrom <= startDate.Value && b.BookingDateTo >= endDate.Value));
			}

			// New filters
			if (withDriver.HasValue) query = query.Where(b => b.WithDriver == withDriver);
			if (withFuel.HasValue) query = query.Where(b => b.WithFuel == withFuel);
			if (!string.IsNullOrEmpty(tripType)) query = query.Where(b => b.TripType == tripType);

			// Join Booking with related tables
			var bookingDetails = from booking in query
								 join vehicle in _db.Vehicle on booking.VehicleId equals vehicle.VehicleId into vehicleJoin
								 from vehicle in vehicleJoin.DefaultIfEmpty()
								 join driver in _db.Driver on booking.DriverId equals driver.DriverId into driverJoin
								 from driver in driverJoin.DefaultIfEmpty()
								 join client in _db.Client on booking.ClientId equals client.ClientId into clientJoin
								 from client in clientJoin.DefaultIfEmpty()
								 join priceMaster in _db.PriceMaster on booking.VehicleId equals priceMaster.VehicleId into priceMasterJoin
								 from priceMaster in priceMasterJoin.DefaultIfEmpty()
								 select new
								 {
									 booking.BookingId,
									 booking,
									 Vehicle = vehicle != null ? new { vehicle } : null,
									 Driver = driver != null ? new { driver } : null,
									 Client = client != null ? new { client } : null,
									 PriceMaster = priceMaster != null ? new { priceMaster } : null,
								 };

			// Apply sorting
			bookingDetails = sortOrder.ToLower() == "asc"
				? bookingDetails.OrderBy(b => b.BookingId)
				: bookingDetails.OrderByDescending(b => b.BookingId);

			// Pagination
			int totalItems = bookingDetails.Count();
			int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
			var bookings = bookingDetails.Skip((currentPageNumber - 1) * pageSize)
									   .Take(pageSize)
									   .ToList();

			// Prepare paginated response
			var response = new
			{
				CurrentPageNumber = currentPageNumber,
				PageSize = pageSize,
				TotalItems = totalItems,
				TotalPages = totalPages,
				BranchId = branchIds,
				Items = bookings
			};

			return Ok(response);
		}
	}

	public class DriverReportDto
	{
		public int DriverId { get; set; }
		public string DriverName { get; set; }
		public int TotalDays { get; set; }
		public int BranchId { get; set; }
		public int BusyDays { get; set; }
		public int LeaveDays { get; set; }
		public int UnusedDays { get; set; }
		public decimal Revenue { get; set; }
		public double BookingRatio { get; set; }
		public string BranchCurrency { get; set; }
		public string TargetCurrency { get; set; }
		public decimal OriginalRevenue { get; set; }
		public decimal OriginalMaintenanceCost { get; set; }
	}

	internal class Interval
	{
		public DateTime Start { get; set; }
		public DateTime End { get; set; }
	}

	public class VehicleReportDto
	{
		public int VehicleId { get; set; }
		public string VehicleName { get; set; }
		public int BranchId { get; set; }
		public string VehicleModel { get; set; }
		public string VehicleCompany { get; set; }
		public string VehicleType { get; set; }
		public string VehicleNumber { get; set; }
		public int TotalDays { get; set; }
		public int BusyDays { get; set; }
		public int UnusedDays { get; set; }
		public decimal Revenue { get; set; }
		public decimal MaintenanceCost { get; set; }
		public string BranchCurrency { get; set; }
		public string TargetCurrency { get; set; }
		public decimal OriginalRevenue { get; set; }
		public decimal OriginalMaintenanceCost { get; set; }
		public double BookingRatio { get; set; }
		public int BookingsWithDriver { get; set; }
		public int BookingsWithoutDriver { get; set; }
		public int TotalBookings { get; set; }
		public int BookingsWithoutFuel { get; set; }
		public int BookingsWithFuel { get; set; }
		public int MaintenanceDays { get; set; }
	}
}