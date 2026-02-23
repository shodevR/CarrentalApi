using CarRentalApi.Data;
using CarRentalApi.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System;
using Microsoft.AspNetCore.Http.HttpResults;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using CarRentalApi.Service;
using Microsoft.EntityFrameworkCore;

namespace CarRentalApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : BaseController
    {
		
		private readonly ApplicationDbContext _db;
		private readonly ICurrencyConversionService _currencyService;

		public BookingController(ApplicationDbContext db
		  , IHttpContextAccessor contextAccessor, IConfiguration configuration, IWebHostEnvironment hostingEnvironment, ICurrencyConversionService currencyService) 
              : base(hostingEnvironment, contextAccessor, configuration, db )
        {
            _db = db;
			_currencyService = currencyService;
		

		}


		[HttpGet]
		public async Task<IActionResult> GetBookings(
	 [FromQuery] string searchText = "",
	 [FromQuery] string branchIds = "",
	 int clientId = 0,
	 int driverId = 0,
	 int vehicleId = 0,
	 DateTime? startDate = null,
	 DateTime? endDate = null,
	 int currentPageNumber = 1,
	 int pageSize = 50,
	 string sortOrder = "desc",
	 string toCurrency = "USD")
		{
			var query = _db.Booking.AsQueryable();

			// Filter conditions
			if (!string.IsNullOrEmpty(branchIds))
			{
				var branchIdList = branchIds.Split(',')
											.Select(int.Parse)
											.ToList();
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

			// Join Booking with related tables
			var bookingDetails = from booking in query
								 join vehicle in _db.Vehicle on booking.VehicleId equals vehicle.VehicleId into vehicleJoin
								 from vehicle in vehicleJoin.DefaultIfEmpty()
								 join driver in _db.Driver on booking.DriverId equals driver.DriverId into driverJoin
								 from driver in driverJoin.DefaultIfEmpty()
								 join client in _db.Client on booking.ClientId equals client.ClientId into clientJoin
								 from client in clientJoin.DefaultIfEmpty()
								 join priceMaster in _db.PriceMaster on booking.VehicleId equals priceMaster.VehicleId into priceMasterJoin
								 from pm in priceMasterJoin.DefaultIfEmpty()
								 select new
								 {
									 booking,
									 Vehicle = vehicle,
									 Driver = driver,
									 Client = client,
									 PriceMaster = pm,
									 BranchId = booking.BranchId,
									 CurrencyCode = booking.CurrencyCode,
									 ROI = booking.ROI
								 };

			// Get distinct branch IDs from the results
			var branchIdsInResults = await bookingDetails
				.Select(b => b.BranchId)
				.Distinct()
				.ToListAsync();

			// Get branch currencies for fallback
			var branchCurrencies = await _db.LocationMaster
				.Where(b => branchIdsInResults.Contains(b.Id))
				.Select(b => new { b.Id, Currency = b.CurrencyCode ?? "USD" })
				.ToDictionaryAsync(b => b.Id, b => b.Currency);

			// Apply sorting
			bookingDetails = sortOrder.ToLower() == "asc"
				? bookingDetails.OrderBy(b => b.booking.BookingId)
				: bookingDetails.OrderByDescending(b => b.booking.BookingId);

			// Pagination
			int totalItems = await bookingDetails.CountAsync();
			int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
			var pagedBookings = await bookingDetails.Skip((currentPageNumber - 1) * pageSize)
												  .Take(pageSize)
												  .ToListAsync();

			// Convert amounts to target currency
			var convertedBookings = pagedBookings.Select(b =>
			{
				// Get conversion rate for this booking
				decimal rate = 1.0m;

				// First try to use booking's currency and ROI
				if (!string.IsNullOrEmpty(b.CurrencyCode) && b.ROI.HasValue && b.ROI.Value > 0)
				{
					if (b.CurrencyCode == toCurrency)
					{
						rate = 1.0m;
					}
					else
					{
						// ROI is the rate for 1 USD to local currency, so we need to invert it
						// to get local currency to USD
						rate = 1.0m / b.ROI.Value;
					}
				}
				else
				{
					// Fallback to branch currency
					var branchCurrency = branchCurrencies.GetValueOrDefault(b.BranchId, "USD");
					if (branchCurrency != toCurrency)
					{
						try
						{
							rate = _currencyService.GetConversionRateAsync(branchCurrency, toCurrency).Result;
						}
						catch
						{
							rate = 1.0m;
						}
					}
				}

				return new
				{
					b.booking.BookingId,
					b.booking.ClientId,
					b.booking.BranchId,
					b.booking.ExistingClientId,
					b.booking.ExistingClientName,
					b.booking.Date,
					b.booking.VehicleName,
					b.booking.VehicleNumber,
					b.booking.DriverLicense,
					b.booking.DriverName,
					b.booking.DriverNumber,
					b.booking.IsInHouse,
					b.booking.BookingDateFrom,
					b.booking.BookingDateTo,
					b.booking.Destination,
					b.booking.Arival,
					b.booking.PaymentOption,
					b.booking.WithDriver,
					b.booking.VehicleId,
					DriverPrice = b.booking.DriverPrice * rate,
					VehiclePrice = b.booking.VehiclePrice * rate,
					b.booking.Discount,
					Amount = b.booking.Amount * rate,
					b.booking.ServiceTime,
					b.booking.TripType,
					b.booking.DriverImage,
					b.booking.VehicleImage,
					b.booking.WithFuel,
					b.booking.Document,
					b.booking.CreatedBy,
					b.booking.CreatedByName,
					b.booking.StatusFlag,
					b.booking.DriverId,
					Vehicle = b.Vehicle,
					Driver = b.Driver,
					Client = b.Client,
					PriceMaster = b.PriceMaster != null ? new
					{
						WithinCity = b.PriceMaster.WithinCity * (double)rate,
						OutsideCity = b.PriceMaster.OutsideCity * (double)rate,
						b.PriceMaster.WeekDiscount,
						b.PriceMaster.MonthDiscount,
						AirportDay = b.PriceMaster.AirportDay * (double)rate,
						AirportNight = b.PriceMaster.AirportNight * (double)rate,
						WithoutFuelWithinCity = b.PriceMaster.WithoutFuelWithinCity * (double)rate,
						WithoutFuelOutsideCity = b.PriceMaster.WithoutFuelOutsideCity * (double)rate,
						b.PriceMaster.WithoutFuelWeekDiscount,
						b.PriceMaster.WithoutFuelMonthDiscount,
						WithoutFuelAirportDay = b.PriceMaster.WithoutFuelAirportDay * (double)rate,
						WithoutFuelAirportNight = b.PriceMaster.WithoutFuelAirportNight * (double)rate
					} : null
				};
			}).ToList();

			// Prepare response
			var response = new
			{
				CurrentPageNumber = currentPageNumber,
				PageSize = pageSize,
				TotalItems = totalItems,
				TotalPages = totalPages,
				BranchIds = branchIds,
				Items = convertedBookings
			};

			return Ok(response);
		}

		[HttpGet("Bookings")]
		public async Task<IActionResult> GetBookingsWithModificationDetails(
			[FromQuery] string searchText = "",
			[FromQuery] string branchIds = "",
			[FromQuery] int clientId = 0,
			[FromQuery] int driverId = 0,
			[FromQuery] int vehicleId = 0,
			[FromQuery] DateTime? startDate = null,
			[FromQuery] DateTime? endDate = null,
			[FromQuery] int currentPageNumber = 1,
			[FromQuery] int pageSize = 50,
			[FromQuery] string sortOrder = "desc",
			[FromQuery] string toCurrency = "USD")
		{
			var bookingQuery = _db.Booking.AsQueryable();

			// Filter conditions
			if (!string.IsNullOrEmpty(branchIds))
			{
				var branchIdList = branchIds.Split(',')
											.Select(int.Parse)
											.ToList();
				bookingQuery = bookingQuery.Where(b => branchIdList.Contains(b.BranchId));
			}
			if (clientId > 0) bookingQuery = bookingQuery.Where(b => b.ClientId == clientId);
			if (driverId > 0) bookingQuery = bookingQuery.Where(b => b.DriverId == driverId);
			if (vehicleId > 0) bookingQuery = bookingQuery.Where(b => b.VehicleId == vehicleId);
			if (startDate.HasValue && endDate.HasValue)
			{
				bookingQuery = bookingQuery.Where(b =>
					(b.BookingDateFrom >= startDate.Value && b.BookingDateFrom <= endDate.Value) ||
					(b.BookingDateTo >= startDate.Value && b.BookingDateTo <= endDate.Value) ||
					(b.BookingDateFrom <= startDate.Value && b.BookingDateTo >= endDate.Value));
			}

			// Join Booking with related tables
			var bookingDetails = from booking in bookingQuery
								 join modify in _db.BookingModify on booking.BookingId equals modify.BookingId into modifyJoin
								 from modify in modifyJoin.DefaultIfEmpty()
								 join vehicle in _db.Vehicle on booking.VehicleId equals vehicle.VehicleId into vehicleJoin
								 from vehicle in vehicleJoin.DefaultIfEmpty()
								 join driver in _db.Driver on booking.DriverId equals driver.DriverId into driverJoin
								 from driver in driverJoin.DefaultIfEmpty()
								 join client in _db.Client on booking.ClientId equals client.ClientId into clientJoin
								 from client in clientJoin.DefaultIfEmpty()
								 join priceMaster in _db.PriceMaster on booking.VehicleId equals priceMaster.VehicleId into priceMasterJoin
								 from pm in priceMasterJoin.DefaultIfEmpty()
								 select new
								 {
									 booking,
									 modify,
									 vehicle,
									 driver,
									 client,
									 pm,
									 BranchId = booking.BranchId,
									 CurrencyCode = booking.CurrencyCode,
									 ROI = booking.ROI
								 };

			// Apply search text filter
			if (!string.IsNullOrWhiteSpace(searchText))
			{
				searchText = searchText.ToLower();
				bookingDetails = bookingDetails.Where(b =>
					(b.client != null && b.client.FirstName.ToLower().Contains(searchText)) ||
					(b.driver != null && b.driver.DriverName.ToLower().Contains(searchText)) ||
					(b.vehicle != null && b.vehicle.VehicleName.ToLower().Contains(searchText)) ||
					(b.booking.ExistingClientName != null && b.booking.ExistingClientName.ToLower().Contains(searchText)) ||
					(b.booking.Destination != null && b.booking.Destination.ToLower().Contains(searchText)));
			}

			// Apply sorting
			bookingDetails = sortOrder.ToLower() == "asc"
				? bookingDetails.OrderBy(b => b.booking.BookingId)
				: bookingDetails.OrderByDescending(b => b.booking.BookingId);

			// Get distinct branch IDs from the results
			var branchIdsInResults = await bookingDetails
				.Select(b => b.BranchId)
				.Distinct()
				.ToListAsync();

			// Get branch currencies for fallback
			var branchCurrencies = await _db.LocationMaster
				.Where(b => branchIdsInResults.Contains(b.Id))
				.Select(b => new { b.Id, Currency = b.CurrencyCode ?? "USD" })
				.ToDictionaryAsync(b => b.Id, b => b.Currency);

			// Pagination
			int totalItems = await bookingDetails.CountAsync();
			int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
			var pagedBookings = await bookingDetails.Skip((currentPageNumber - 1) * pageSize)
												  .Take(pageSize)
												  .ToListAsync();

			// Convert amounts to target currency
			var convertedBookings = pagedBookings.Select(b =>
			{
				// Get conversion rate for this booking
				decimal rate = 1.0m;

				// First try to use booking's currency and ROI
				if (!string.IsNullOrEmpty(b.CurrencyCode) && b.ROI.HasValue && b.ROI.Value > 0)
				{
					if (b.CurrencyCode == toCurrency)
					{
						rate = 1.0m;
					}
					else
					{
						// ROI is the rate for 1 USD to local currency, so we need to invert it
						// to get local currency to USD
						rate = 1.0m / b.ROI.Value;
					}
				}
				else
				{
					// Fallback to branch currency
					var branchCurrency = branchCurrencies.GetValueOrDefault(b.BranchId, "USD");
					if (branchCurrency != toCurrency)
					{
						try
						{
							rate = _currencyService.GetConversionRateAsync(branchCurrency, toCurrency).Result;
						}
						catch
						{
							rate = 1.0m;
						}
					}
				}

				return new
				{
					b.booking.BookingId,
					b.booking.ClientId,
					b.booking.BranchId,
					b.booking.ExistingClientId,
					b.booking.ExistingClientName,
					b.booking.Date,
					b.booking.BookingDateFrom,
					b.booking.BookingDateTo,
					b.booking.Destination,
					b.booking.Arival,
					b.booking.VehicleName,
					b.booking.VehicleNumber,
					b.booking.DriverLicense,
					b.booking.DriverName,
					b.booking.DriverNumber,
					b.booking.IsInHouse,
					b.booking.PaymentOption,
					b.booking.WithDriver,
					b.booking.VehicleId,
					DriverPrice = b.booking.DriverPrice * rate,
					VehiclePrice = b.booking.VehiclePrice * rate,
					b.booking.Discount,
					Amount = b.booking.Amount * rate,
					b.booking.ServiceTime,
					b.booking.TripType,
					b.booking.DriverImage,
					b.booking.VehicleImage,
					b.booking.WithFuel,
					b.booking.Document,
					b.booking.CreatedBy,
					b.booking.CreatedByName,
					b.booking.StatusFlag,
					IsModified = b.modify != null,
					ModificationDetails = b.modify,
					b.vehicle,
					b.booking.DriverId,
					b.driver,
					b.client,
					PriceMaster = b.pm != null ? new
					{
						WithinCity = b.pm.WithinCity * (double)rate,
						OutsideCity = b.pm.OutsideCity * (double)rate,
						b.pm.WeekDiscount,
						b.pm.MonthDiscount,
						AirportDay = b.pm.AirportDay * (double)rate,
						AirportNight = b.pm.AirportNight * (double)rate,
						WithoutFuelWithinCity = b.pm.WithoutFuelWithinCity * (double)rate,
						WithoutFuelOutsideCity = b.pm.WithoutFuelOutsideCity * (double)rate,
						b.pm.WithoutFuelWeekDiscount,
						b.pm.WithoutFuelMonthDiscount,
						WithoutFuelAirportDay = b.pm.WithoutFuelAirportDay * (double)rate,
						WithoutFuelAirportNight = b.pm.WithoutFuelAirportNight * (double)rate
					} : null
				};
			}).ToList();

			// Prepare response
			var response = new
			{
				CurrentPageNumber = currentPageNumber,
				PageSize = pageSize,
				TotalItems = totalItems,
				TotalPages = totalPages,
				BranchIds = branchIds,
				Items = convertedBookings
			};

			return Ok(response);
		}

		[HttpGet("GetBookingDetails")]
		public async Task<IActionResult> GetBookingDetails(
			[FromQuery] string searchText = "",
			[FromQuery] int branchId = 0,
			[FromQuery] int clientId = 0,
			[FromQuery] int driverId = 0,
			[FromQuery] int vehicleId = 0,
			[FromQuery] DateTime? startDate = null,
			[FromQuery] DateTime? endDate = null,
			[FromQuery] int currentPageNumber = 1,
			[FromQuery] int pageSize = 50,
			[FromQuery] string sortOrder = "desc",
			[FromQuery] string toCurrency = "USD")
		{
			var query = from booking in _db.Booking
						join driver in _db.Driver on booking.DriverId equals driver.DriverId
						join vehicle in _db.Vehicle on booking.VehicleId equals vehicle.VehicleId
						select new
						{
							booking,
							driver,
							vehicle,
							BranchId = booking.BranchId,
							CurrencyCode = booking.CurrencyCode,
							ROI = booking.ROI
						};

			// Apply filters
			if (branchId > 0) query = query.Where(b => b.BranchId == branchId);
			if (!string.IsNullOrEmpty(searchText))
			{
				query = query.Where(b =>
					b.vehicle.VehicleName.Contains(searchText) ||
					b.driver.DriverName.Contains(searchText));
			}
			if (clientId > 0) query = query.Where(b => b.booking.ClientId == clientId);
			if (driverId > 0) query = query.Where(b => b.booking.DriverId == driverId);
			if (vehicleId > 0) query = query.Where(b => b.booking.VehicleId == vehicleId);
			if (startDate.HasValue && endDate.HasValue)
			{
				query = query.Where(b => b.booking.BookingDateFrom >= startDate.Value &&
										 b.booking.BookingDateTo <= endDate.Value);
			}

			// Apply sorting
			query = sortOrder.ToLower() == "asc"
				? query.OrderBy(b => b.booking.BookingId)
				: query.OrderByDescending(b => b.booking.BookingId);

			// Pagination
			int totalItems = await query.CountAsync();
			int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
			var bookings = await query.Skip((currentPageNumber - 1) * pageSize)
									 .Take(pageSize)
									 .ToListAsync();

			// Get branch currencies for fallback
			var branchIdsInResults = bookings.Select(b => b.BranchId).Distinct().ToList();
			var branchCurrencies = await _db.LocationMaster
				.Where(b => branchIdsInResults.Contains(b.Id))
				.Select(b => new { b.Id, Currency = b.CurrencyCode ?? "USD" })
				.ToDictionaryAsync(b => b.Id, b => b.Currency);

			// Convert booking amounts to target currency
			var convertedBookings = bookings.Select(b =>
			{
				// Get conversion rate for this booking
				decimal rate = 1.0m;

				// First try to use booking's currency and ROI
				if (!string.IsNullOrEmpty(b.CurrencyCode) && b.ROI.HasValue && b.ROI.Value > 0)
				{
					if (b.CurrencyCode == toCurrency)
					{
						rate = 1.0m;
					}
					else
					{
						// ROI is the rate for 1 USD to local currency, so we need to invert it
						// to get local currency to USD
						rate = 1.0m / b.ROI.Value;
					}
				}
				else
				{
					// Fallback to branch currency
					var branchCurrency = branchCurrencies.GetValueOrDefault(b.BranchId, "USD");
					if (branchCurrency != toCurrency)
					{
						try
						{
							rate = _currencyService.GetConversionRateAsync(branchCurrency, toCurrency).Result;
						}
						catch
						{
							rate = 1.0m;
						}
					}
				}

				return new
				{
					b.booking.BookingId,
					b.booking.ClientId,
					b.vehicle.VehicleId,
					VehicleName = b.vehicle.VehicleName,
					b.driver.DriverId,
					DriverName = b.driver.DriverName,
					b.booking.BranchId,
					Booking = new
					{
						b.booking.BookingId,
						b.booking.ClientId,
						b.booking.BranchId,
						b.booking.ExistingClientId,
						b.booking.ExistingClientName,
						b.booking.VehicleName,
						b.booking.VehicleNumber,
						b.booking.DriverLicense,
						b.booking.DriverName,
						b.booking.DriverNumber,
						b.booking.IsInHouse,
						b.booking.Date,
						b.booking.BookingDateFrom,
						b.booking.BookingDateTo,
						b.booking.Destination,
						b.booking.Arival,
						b.booking.PaymentOption,
						b.booking.WithDriver,
						b.booking.VehicleId,
						b.booking.DriverImage,
						b.booking.VehicleImage,
						DriverPrice = b.booking.DriverPrice * rate,
						VehiclePrice = b.booking.VehiclePrice * rate,
						b.booking.Discount,
						Amount = b.booking.Amount * rate,
						b.booking.ServiceTime,
						b.booking.TripType,
						b.booking.WithFuel,
						b.booking.Document,
						b.booking.CreatedBy,
						b.booking.CreatedByName,
						b.booking.StatusFlag,
						b.booking.DriverId
					}
				};
			}).ToList();

			// Response
			var response = new
			{
				CurrentPageNumber = currentPageNumber,
				PageSize = pageSize,
				TotalItems = totalItems,
				TotalPages = totalPages,
				BranchId = branchId,
				Items = convertedBookings
			};

			return Ok(response);
		}

		[HttpGet("GetBookingDetailsById")]
		public async Task<IActionResult> GetBookingDetailsById(
			[FromQuery] int bookingId,
			[FromQuery] string toCurrency = "USD")
		{
			var result = await (from booking in _db.Booking
								join driver in _db.Driver on booking.DriverId equals driver.DriverId
								join vehicle in _db.Vehicle on booking.VehicleId equals vehicle.VehicleId
								where booking.BookingId == bookingId
								select new
								{
									booking,
									driver,
									vehicle,
									BranchId = booking.BranchId,
									CurrencyCode = booking.CurrencyCode,
									ROI = booking.ROI
								}).FirstOrDefaultAsync();

			if (result == null)
			{
				return Ok(new { message = "Booking not found." });
			}

			// Get conversion rate for this booking
			decimal rate = 1.0m;

			// First try to use booking's currency and ROI
			if (!string.IsNullOrEmpty(result.CurrencyCode) && result.ROI.HasValue && result.ROI.Value > 0)
			{
				if (result.CurrencyCode == toCurrency)
				{
					rate = 1.0m;
				}
				else
				{
					// ROI is the rate for 1 USD to local currency, so we need to invert it
					// to get local currency to USD
					rate = 1.0m / result.ROI.Value;
				}
			}
			else
			{
				// Fallback to branch currency
				var branchCurrency = await _db.LocationMaster
					.Where(b => b.Id == result.BranchId)
					.Select(b => b.CurrencyCode ?? "USD")
					.FirstOrDefaultAsync();

				if (branchCurrency != toCurrency)
				{
					rate = await _currencyService.GetConversionRateAsync(branchCurrency, toCurrency);
				}
			}

			return Ok(new
			{
				result.booking.BookingId,
				result.booking.ClientId,
				result.vehicle.VehicleId,
				VehicleName = result.vehicle.VehicleName,
				result.driver.DriverId,
				DriverName = result.driver.DriverName,
				Booking = new
				{
					result.booking.BookingId,
					result.booking.ClientId,
					result.booking.BranchId,
					result.booking.ExistingClientId,
					result.booking.ExistingClientName,
					result.booking.VehicleName,
					result.booking.VehicleNumber,
					result.booking.DriverLicense,
					result.booking.DriverName,
					result.booking.DriverNumber,
					result.booking.IsInHouse,
					result.booking.Date,
					result.booking.BookingDateFrom,
					result.booking.BookingDateTo,
					result.booking.Destination,
					result.booking.Arival,
					result.booking.PaymentOption,
					result.booking.WithDriver,
					result.booking.VehicleId,
					result.booking.DriverImage,
					result.booking.VehicleImage,
					DriverPrice = result.booking.DriverPrice * rate,
					VehiclePrice = result.booking.VehiclePrice * rate,
					result.booking.Discount,
					Amount = result.booking.Amount * rate,
					result.booking.ServiceTime,
					result.booking.TripType,
					result.booking.WithFuel,
					result.booking.Document,
					result.booking.CreatedBy,
					result.booking.CreatedByName,
					result.booking.StatusFlag,
					result.booking.DriverId
				}
			});
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetBookingById(
			[FromRoute] int id,
			[FromQuery] string toCurrency = "USD")
		{
			var result = await (from booking in _db.Booking
								join driver in _db.Driver on booking.DriverId equals driver.DriverId into driverJoin
								from driver in driverJoin.DefaultIfEmpty()
								join vehicle in _db.Vehicle on booking.VehicleId equals vehicle.VehicleId
								join client in _db.Client on booking.ClientId equals client.ClientId into clientJoin
								from client in clientJoin.DefaultIfEmpty()
								where booking.BookingId == id
								select new
								{
									booking,
									driver,
									vehicle,
									client,
									BranchId = booking.BranchId,
									CurrencyCode = booking.CurrencyCode,
									ROI = booking.ROI
								}).FirstOrDefaultAsync();

			if (result == null)
			{
				return NotFound(new { message = "Booking not found." });
			}

			// Get conversion rate for this booking
			decimal rate = 1.0m;

			// First try to use booking's currency and ROI
			if (!string.IsNullOrEmpty(result.CurrencyCode) && result.ROI.HasValue && result.ROI.Value > 0)
			{
				if (result.CurrencyCode == toCurrency)
				{
					rate = 1.0m;
				}
				else
				{
					// ROI is the rate for 1 USD to local currency, so we need to invert it
					// to get local currency to USD
					rate = 1.0m / result.ROI.Value;
				}
			}
			else
			{
				// Fallback to branch currency
				var branchCurrency = await _db.LocationMaster
					.Where(b => b.Id == result.BranchId)
					.Select(b => b.CurrencyCode ?? "USD")
					.FirstOrDefaultAsync();

				if (branchCurrency != toCurrency)
				{
					rate = await _currencyService.GetConversionRateAsync(branchCurrency, toCurrency);
				}
			}
			var branchCurrencies = await _db.LocationMaster
			.ToDictionaryAsync(b => b.Id, b => b.CurrencyCode ?? "USD");

			// Convert amounts to target currency
			var response = new
			{
				result.booking.BookingId,
				result.booking.ClientId,
				result.booking.BranchId,
				result.booking.ExistingClientId,
				result.booking.ExistingClientName,
				result.booking.Date,
				result.booking.BookingDateFrom,
				result.booking.BookingDateTo,
				result.booking.Destination,
				result.booking.Arival,
				result.booking.VehicleName,
				result.booking.VehicleNumber,
				result.booking.DriverLicense,
				result.booking.DriverName,
				result.booking.DriverNumber,
				result.booking.IsInHouse,
				result.booking.DriverImage,
				result.booking.VehicleImage,
				result.booking.PaymentOption,
				result.booking.WithDriver,
				result.booking.VehicleId,
				DriverPrice = result.booking.DriverPrice * rate,
				VehiclePrice = result.booking.VehiclePrice * rate,
				Amount = result.booking.Amount * rate,
				OriginalDriverPrice = result.booking.DriverPrice,
				OriginalVehiclePrice = result.booking.VehiclePrice,
				OriginalAmount = result.booking.Amount,
				result.booking.Discount,
				result.booking.ServiceTime,
				result.booking.TripType,
				result.booking.WithFuel,
				result.booking.Document,
				result.booking.CreatedBy,
				result.booking.CreatedByName,
				result.booking.StatusFlag,
				Vehicle = result.vehicle,
				Client = result.client,
				Driver = result.driver,
				CurrencyInfo = new
				{
					FromCurrency = result.CurrencyCode ?? branchCurrencies.GetValueOrDefault(result.BranchId, "USD"),
					ToCurrency = toCurrency,
					ConversionRate = rate
				}
			};

			return Ok(response);
		}


		[HttpPost]
		public async Task<IActionResult> SaveBooking([FromBody] BookingDTO booking)
        {
			if (booking.ROI == null)
			{
				decimal conversionRate = await _currencyService.GetConversionRateAsync(booking.FromCurrency, "USD");
				booking.ROI = conversionRate;
			}



			if (booking == null)
                return Ok("Invalid booking data.");

            try
            {
				// Check for vehicle conflicts using effective end date
				var vehicleConflict = _db.Booking
	                .Where(b => b.StatusFlag == true && b.VehicleId == booking.VehicleId)
	                .Where(b =>
		                !_db.CheckList
			                .Where(cl => cl.BookingId == b.BookingId)
			                .OrderByDescending(cl => cl.CheckListId)
			                .Select(cl => cl.OdometerAfter)
			                .Take(1)
			                .Any(odo => odo > 0) // only include conflicts if checklist is missing or incomplete
	                )
	                .Select(b => new {
		                b.BookingDateFrom,
		                EffectiveEndDate = _db.CheckList
			                .Where(cl => cl.BookingId == b.BookingId)
			                .OrderByDescending(cl => cl.CheckListId)
			                .Select(cl => (DateTime?)cl.DateOfReadingAfter)
			                .FirstOrDefault() ?? b.BookingDateTo
	                })
	                .Any(b => booking.BookingDateFrom <= b.EffectiveEndDate &&
			                 booking.BookingDateTo >= b.BookingDateFrom);


				// Check for driver conflicts only if driver is assigned
				if (booking.DriverId > 0)
                {
					var driverConflict = _db.Booking
	                 .Where(b => b.StatusFlag == true && b.DriverId == booking.DriverId)
	                 .Where(b =>
		                 !_db.CheckList
			                 .Where(cl => cl.BookingId == b.BookingId)
			                 .OrderByDescending(cl => cl.CheckListId)
			                 .Select(cl => cl.OdometerAfter)
			                 .Take(1)
			                 .Any(odo => odo > 0) // only include conflicts if checklist is missing or incomplete
	                 )
	                 .Select(b => new {
		                 b.BookingDateFrom,
		                 EffectiveEndDate = _db.CheckList
			                 .Where(cl => cl.BookingId == b.BookingId)
			                 .OrderByDescending(cl => cl.CheckListId)
			                 .Select(cl => (DateTime?)cl.DateOfReadingAfter)
			                 .FirstOrDefault() ?? b.BookingDateTo
	                 })
	                 .Any(b => booking.BookingDateFrom <= b.EffectiveEndDate &&
			                  booking.BookingDateTo >= b.BookingDateFrom);

				}


				// Handle ClientDTO saving if it exists
				if (booking.Client != null)
                {
                    int ClientRegID = 0;
                    if (booking.Client.ClientId != null && booking.Client.ClientId > 0) {
                        ClientRegID = booking.Client.ClientId.Value;


                    }
                    // Map the ClientDTO to a Client entity
                    var client = new Client
                    {
                        
                        ClientId = ClientRegID,
                        BranchId = booking.Client.BranchId,
                        FirstName = booking.Client.FirstName,
                        LastName = booking.Client.LastName,
                        Email = booking.Client.Email,
                        Mobile = booking.Client.Mobile,
                        Date = booking.Client.Date ?? DateTime.Now,
                        ReferedBy = booking.Client.ReferedBy,
                        BusinessProposal = booking.Client.BusinessProposal,
                        CompanyName = booking.Client.CompanyName,
                        CompanyAddress = booking.Client.CompanyAddress,
                        ComapanyType = booking.Client.ComapanyType,
                        Designation = booking.Client.Designation,
                        CreatedBy = (int)this.UserID,
                        CreatedByName = this.UserEmail,
                        StatusFlag = booking.Client.StatusFlag ?? true
                    };
                    if(ClientRegID > 0)
                    {
                        _db.Client.Update(client);
                    }
                    else
                    {
                        _db.Client.Add(client);
                    }
                    // Save client to the database
                   
                    _db.SaveChanges();
                    // Associate the newly created client with the booking
                    booking.ClientId = client.ClientId;
                }

                // Save booking
                var booking2 = new Booking
                {
                    BookingId = 0,
                    VehicleId = booking.VehicleId,
                    BranchId = booking.BranchId,
                    DriverId = booking.DriverId,
                    ClientId = booking.ClientId,
                    ExistingClientId = booking.ExistingClientId,
                    ExistingClientName = booking.ExistingClientName,
                    DriverName = booking.DriverName,
                    DriverLicense = booking.DriverLicense,
                    DriverNumber = booking.DriverNumber,
                    VehicleName = booking.VehicleName,
                    VehicleNumber = booking.VehicleNumber,
                    IsInHouse = booking.IsInHouse,
                    Date = DateTime.Now,
                    BookingDateFrom = booking.BookingDateFrom,
                    BookingDateTo = booking.BookingDateTo,
                    Destination = booking.Destination,
                    Arival = booking.Arival,
					DriverImage=booking.DriverImage,
					VehicleImage=booking.VehicleImage,
					PaymentOption = booking.PaymentOption,
                    WithDriver = booking.WithDriver,
                    DriverPrice = booking.DriverPrice,
                    VehiclePrice = booking.VehiclePrice,
                    Discount = booking.Discount,
                    TripType= booking.TripType,
                    ServiceTime = booking.ServiceTime,
                    Amount = booking.Amount,
                    WithFuel = booking.WithFuel,
                    Document = booking.Document,
                    CreatedBy = (int)this.UserID,
                    CreatedByName = this.UserEmail,
                    CurrencyCode = booking.CurrencyCode,
                    ROI =booking.ROI,
                    StatusFlag = true
                };

                _db.Booking.Add(booking2);
                _db.SaveChanges();

                return Ok(new { message = "Booking saved successfully!", bookingId = booking2.BookingId });
            }
            catch (Exception ex)
            {
                return Ok( $"Internal server error: {ex.Message}");
            }
        }



        [HttpPost("update/{id}")]
        public async Task<IActionResult> UpdateBooking(int id, [FromBody] BookingDTO booking)
        {
			
			
            if (booking == null || booking.BookingId != id)
                return BadRequest("Invalid booking data or ID mismatch.");

            var existingBooking = _db.Booking.FirstOrDefault(b => b.BookingId == id);
            if (existingBooking == null)
                return Ok("Booking not found.");

            try
            {
				
				// Update only non-default values or non-null/empty values
				existingBooking.VehicleId = booking.VehicleId != 0 ? booking.VehicleId : existingBooking.VehicleId;
                existingBooking.BranchId = booking.BranchId != 0 ? booking.BranchId : existingBooking.BranchId;
                existingBooking.DriverId = booking.DriverId != 0 ? booking.DriverId : existingBooking.DriverId;
                existingBooking.ClientId = booking.ClientId != 0 ? booking.ClientId : existingBooking.ClientId;
                existingBooking.ExistingClientName = !string.IsNullOrEmpty(booking.ExistingClientName) ? booking.ExistingClientName : existingBooking.ExistingClientName;
                existingBooking.Date = booking.Date != default ? booking.Date : existingBooking.Date;
                existingBooking.BookingDateFrom = booking.BookingDateFrom != default ? booking.BookingDateFrom : existingBooking.BookingDateFrom;
                existingBooking.BookingDateTo = booking.BookingDateTo != default ? booking.BookingDateTo : existingBooking.BookingDateTo;
                existingBooking.Destination = !string.IsNullOrEmpty(booking.Destination) ? booking.Destination : existingBooking.Destination;
                existingBooking.Arival = !string.IsNullOrEmpty(booking.Arival) ? booking.Arival : existingBooking.Arival;
                existingBooking.PaymentOption = !string.IsNullOrEmpty(booking.PaymentOption) ? booking.PaymentOption : existingBooking.PaymentOption;
                existingBooking.WithDriver = booking.WithDriver;
                existingBooking.ServiceTime = !string.IsNullOrEmpty(booking.Arival) ? booking.Arival : existingBooking.Arival;
                existingBooking.TripType = !string.IsNullOrEmpty(booking.TripType) ? booking.TripType : existingBooking.TripType;
                existingBooking.DriverPrice = booking.DriverPrice != default ? booking.DriverPrice : existingBooking.DriverPrice;
                existingBooking.VehiclePrice = booking.VehiclePrice != default ? booking.VehiclePrice : existingBooking.VehiclePrice;
                existingBooking.Discount = booking.Discount != default ? booking.Discount : existingBooking.Discount;
                existingBooking.Amount = booking.Amount != default ? booking.Amount : existingBooking.Amount;
                existingBooking.DriverNumber = !string.IsNullOrEmpty(booking.DriverNumber) ? booking.DriverNumber : existingBooking.DriverNumber;
                existingBooking.DriverName = !string.IsNullOrEmpty(booking.DriverName) ? booking.DriverName : existingBooking.DriverName;
                existingBooking.DriverLicense = !string.IsNullOrEmpty(booking.DriverLicense) ? booking.DriverLicense : existingBooking.DriverLicense;
                existingBooking.VehicleName = !string.IsNullOrEmpty(booking.VehicleName) ? booking.VehicleName : existingBooking.VehicleName;
                existingBooking.VehicleNumber = !string.IsNullOrEmpty(booking.VehicleNumber) ? booking.VehicleNumber : existingBooking.VehicleNumber;
                existingBooking.WithFuel = booking.WithFuel;
                existingBooking.Document = !string.IsNullOrEmpty(booking.Document) ? booking.Document : existingBooking.Document;
                existingBooking.IsInHouse = booking.IsInHouse;
                existingBooking.CreatedBy = (int)this.UserID;
				existingBooking.DriverImage = !string.IsNullOrEmpty(booking.DriverImage) ? booking.DriverImage : existingBooking.DriverImage;
				existingBooking.VehicleImage = !string.IsNullOrEmpty(booking.VehicleImage) ? booking.VehicleImage : existingBooking.VehicleImage;
				existingBooking.CreatedByName = this.UserEmail;
                existingBooking.StatusFlag = booking.StatusFlag;

                _db.Booking.Update(existingBooking);
                _db.SaveChanges();

                return Ok(new { message = "Booking updated successfully!" });
            }
            catch (Exception ex)
            {
                return Ok( $"Internal server error: {ex.Message}");
            }
        }


        [HttpPost("delete/{id}")]
        public IActionResult DeleteBooking(int id)
        {
            var booking = _db.Booking.FirstOrDefault(b => b.BookingId == id);
            if (booking == null) return Ok(new { message = "Booking not found." });

            try
            {
                _db.Booking.Remove(booking);
                _db.SaveChanges();
                return Ok(new { message = "Booking deleted successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
		[HttpGet("GetPrice")]
		public async Task<IActionResult> GetPrice(int driverId, int vehicleId, string toCurrency = "USD")
		{
			// Get vehicle with branch information
			var vehicle = await _db.Vehicle
				.Where(v => v.VehicleId == vehicleId)
				.Select(v => new { v.BranchId })
				.FirstOrDefaultAsync();

			if (vehicle == null)
			{
				return NotFound(new { Message = "Vehicle not found" });
			}

			// Get driver with branch information
			var driver = await _db.Driver
				.Where(d => d.DriverId == driverId)
				.Select(d => new { d.BranchId })
				.FirstOrDefaultAsync();

			if (driver == null)
			{
				return NotFound(new { Message = "Driver not found" });
			}

			// Get branch currencies
			var branchIds = new List<int> { vehicle.BranchId, driver.BranchId }.Distinct();
			var branchCurrencies = await _db.LocationMaster
				.Where(b => branchIds.Contains(b.Id))
				.Select(b => new { b.Id, Currency = b.CurrencyCode ?? "USD" })
				.ToDictionaryAsync(b => b.Id, b => b.Currency);

			// Get conversion rates
			var uniqueCurrencies = branchCurrencies.Values.Distinct();
			var conversionRates = new Dictionary<string, decimal>();

			foreach (var currency in uniqueCurrencies)
			{
				if (currency != toCurrency)
				{
					conversionRates[currency] = await _currencyService.GetConversionRateAsync(currency, toCurrency);
				}
			}

			// Get vehicle price
			var vehiclePrice = await _db.PriceMaster
				.Where(pm => pm.VehicleId == vehicleId)
				.Select(pm => new {
					pm.WithinCity,
					pm.OutsideCity,
					pm.WithoutFuelWithinCity,
					pm.WithoutFuelOutsideCity
				})
				.FirstOrDefaultAsync();

			if (vehiclePrice == null)
			{
				return NotFound(new { Message = "Vehicle price information not found" });
			}

			// Get driver price
			var driverPrice = await _db.Driver
				.Where(d => d.DriverId == driverId)
				.Select(d => d.PricePerDay)
				.FirstOrDefaultAsync();

			// Calculate conversion rates
			var vehicleBranchCurrency = branchCurrencies.GetValueOrDefault(vehicle.BranchId, "USD");
			var vehicleRate = vehicleBranchCurrency == toCurrency ? 1.0m : conversionRates.GetValueOrDefault(vehicleBranchCurrency, 1.0m);

			var driverBranchCurrency = branchCurrencies.GetValueOrDefault(driver.BranchId, "USD");
			var driverRate = driverBranchCurrency == toCurrency ? 1.0m : conversionRates.GetValueOrDefault(driverBranchCurrency, 1.0m);

			// Prepare response
			var priceDetails = new
			{
				VehiclePriceWithinCity = vehiclePrice.WithinCity * (double)vehicleRate,
				VehiclePriceOutsideCity = vehiclePrice.OutsideCity * (double)vehicleRate,
				VehiclePriceWithoutFuelWithinCity = vehiclePrice.WithoutFuelWithinCity * (double)vehicleRate,
				VehiclePriceWithoutFuelOutsideCity = vehiclePrice.WithoutFuelOutsideCity * (double)vehicleRate,
				DriverPricePerDay = driverPrice * driverRate,
				CurrencyInfo = new
				{
					VehicleCurrency = vehicleBranchCurrency,
					DriverCurrency = driverBranchCurrency,
					TargetCurrency = toCurrency,
					VehicleConversionRate = vehicleRate,
					DriverConversionRate = driverRate
				}
			};

			return Ok(priceDetails);
		}

		[HttpPost("SaveOutHouseBooking")]
        public async Task<IActionResult> SaveOutHouseBooking([FromBody] BookingDTO booking)
        {
            if (booking.ROI == null) { 
            decimal conversionRate = await _currencyService.GetConversionRateAsync(booking.FromCurrency, "USD");
                booking.ROI = conversionRate;
            }
			


			if (booking == null)
                return Ok("Invalid booking data.");

            try
            {

                if (booking.Client != null)
                {
                    int ClientRegID = 0;
                    if (booking.Client.ClientId != null && booking.Client.ClientId > 0)
                    {
                        ClientRegID = booking.Client.ClientId.Value;


                    }
                    // Map the ClientDTO to a Client entity
                    var client = new Client
                    {

                        ClientId = ClientRegID,
                        BranchId = booking.Client.BranchId,
                        FirstName = booking.Client.FirstName,
                        LastName = booking.Client.LastName,
                        Email = booking.Client.Email,
                        Mobile = booking.Client.Mobile,
                        Date = booking.Client.Date ?? DateTime.Now,
                        ReferedBy = booking.Client.ReferedBy,
                        BusinessProposal = booking.Client.BusinessProposal,
                        CompanyName = booking.Client.CompanyName,
                        CompanyAddress = booking.Client.CompanyAddress,
                        ComapanyType = booking.Client.ComapanyType,
                        Designation = booking.Client.Designation,
                        CreatedBy = (int)this.UserID,
                        CreatedByName = this.UserEmail,
                        StatusFlag = booking.Client.StatusFlag ?? true
                    };
                    if (ClientRegID > 0)
                    {
                        _db.Client.Update(client);
                    }
                    else
                    {
                        _db.Client.Add(client);
                    }
                    // Save client to the database

                    _db.SaveChanges();
                    // Associate the newly created client with the booking
                    booking.ClientId = client.ClientId;
                }

                // Save booking
                var booking2 = new Booking
                {
                    BookingId = 0,
                    VehicleId = booking.VehicleId,
                    BranchId = booking.BranchId,
                    DriverId = booking.DriverId,
                    ClientId = booking.ClientId,
                    ExistingClientId = booking.ExistingClientId,
                    ExistingClientName = booking.ExistingClientName,
                    DriverName = booking.DriverName,
                    DriverLicense = booking.DriverLicense,
                    DriverNumber = booking.DriverNumber,
                    VehicleName = booking.VehicleName,
                    VehicleNumber = booking.VehicleNumber,
                    IsInHouse = false,
                    Date = DateTime.Now,
                    BookingDateFrom = booking.BookingDateFrom,
                    BookingDateTo = booking.BookingDateTo,
                    Destination = booking.Destination,
                    Arival = booking.Arival,
                    DriverImage = booking.DriverImage,
                    VehicleImage = booking.VehicleImage,
                    PaymentOption = booking.PaymentOption,
                    WithDriver = booking.WithDriver,
                    DriverPrice = booking.DriverPrice,
                    VehiclePrice = booking.VehiclePrice,
                    Discount = booking.Discount,
                    TripType = booking.TripType,
                    ServiceTime = booking.ServiceTime,
                    Amount = booking.Amount,
                    WithFuel = booking.WithFuel,
                    Document = booking.Document,
                    CreatedBy = (int)this.UserID,
                    CreatedByName = this.UserEmail,
                    CurrencyCode = booking.CurrencyCode,
                    ROI = booking.ROI,
                    StatusFlag = true
                };

                _db.Booking.Add(booking2);
                _db.SaveChanges();

                return Ok(new { message = "Booking saved successfully!", bookingId = booking2.BookingId });
            }
            catch (Exception ex)
            {
                return Ok($"Internal server error: {ex.Message}");
            }
        }

    }
}
