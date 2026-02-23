using CarRentalApi.Data;
using CarRentalApi.Model;
using CarRentalApi.Model.CarRentalApi.Model;
using CarRentalApi.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;

namespace CarRentalApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingModifyController : BaseController
    {
		
		private readonly ApplicationDbContext _db;
		private readonly ICurrencyConversionService _currencyService;

		public BookingModifyController(ApplicationDbContext db
		  , IHttpContextAccessor contextAccessor, IConfiguration configuration, IWebHostEnvironment hostingEnvironment, ICurrencyConversionService currencyService)
			  : base(hostingEnvironment, contextAccessor, configuration, db)
		{
			_db = db;
			_currencyService = currencyService;
			

		}

		// Method to retrieve BookingModify records by BookingId
		[HttpGet("history/{bookingId}")]
		public async Task<IActionResult> GetBookingModificationHistory(int bookingId, [FromQuery] string toCurrency = "USD")
		{
			var modifications = _db.BookingModify
				.Where(b => b.BookingId == bookingId)
				.OrderByDescending(b => b.BookingModifiedId)
				.ToList();

			if (!modifications.Any())
			{
				return NotFound(new { message = "No modification history found for the provided booking." });
			}

			decimal conversionRate = await _currencyService.GetConversionRateAsync("USD", toCurrency);

			var converted = modifications.Select(m => new
			{
				m.BookingModifiedId,
				m.BookingId,
				m.VehicleId,
				m.VehicleName,
				m.VehicleNumber,
				m.BranchId,
				m.DriverId,
				m.DriverName,
				m.DriverNumber,
				m.DriverLicense,
				m.ClientId,
				m.ExistingClientId,
				m.ExistingClientName,
				m.Date,
				m.BookingDateFrom,
				m.BookingDateTo,
				m.Destination,
				m.Arival,
				m.PaymentOption,
				m.WithDriver,
				DriverPrice = m.DriverPrice * conversionRate,
				VehiclePrice = m.VehiclePrice * conversionRate,
				m.TripType,
				m.ServiceTime,
				m.Discount,
				Amount =m.Amount * conversionRate,
				m.WithFuel,
				m.Document,
				m.CreatedBy,
				m.IsInHouse,
				m.CreatedByName,
				m.StatusFlag
			});

			return Ok(converted);
		}


		// Method to log changes before updating the Booking
		[HttpPost("update/{id}")]
		public async Task<IActionResult> UpdateBooking(int id, [FromBody] BookingModify booking)
        {
			
			if (booking == null || booking.BookingId != id)
                return Ok("Invalid booking data or ID mismatch.");

            var existingBooking = _db.Booking.FirstOrDefault(b => b.BookingId == id);
            if (existingBooking == null)
                return Ok("Booking not found.");

            try
            {
                // Save existing booking data to BookingModify table before updating
                var bookingModify = new BookingModify
                {
                    BookingId = existingBooking.BookingId,
                    VehicleId = existingBooking.VehicleId,
                    BranchId = existingBooking.BranchId,
                    DriverId = existingBooking.DriverId,
                    DriverLicense = existingBooking.DriverLicense,
                    DriverNumber = existingBooking.DriverNumber,
                    DriverName = existingBooking.DriverName,
                    VehicleName = existingBooking.VehicleName,
                    IsInHouse = existingBooking.IsInHouse,
                    VehicleNumber= existingBooking.VehicleNumber,
                    
                    ClientId = existingBooking.ClientId,
                    ExistingClientId = existingBooking.ExistingClientId,
                    ExistingClientName = existingBooking.ExistingClientName,
                    Date = existingBooking.Date,
                    BookingDateFrom = existingBooking.BookingDateFrom,
                    BookingDateTo = existingBooking.BookingDateTo,
                    Destination = existingBooking.Destination,
                    Arival = existingBooking.Arival,
                    PaymentOption = existingBooking.PaymentOption,
                    WithDriver = existingBooking.WithDriver,
                    DriverPrice = existingBooking.DriverPrice,
                    VehiclePrice = existingBooking.VehiclePrice,
                    Discount = existingBooking.Discount,
                    Amount = existingBooking.Amount,
                    WithFuel = existingBooking.WithFuel,
                    Document = existingBooking.Document,
                    CreatedBy = existingBooking.CreatedBy,
                    CreatedByName = existingBooking.CreatedByName,
                    TripType = existingBooking.TripType,
                    ServiceTime = existingBooking.ServiceTime,
                    StatusFlag = existingBooking.StatusFlag,
                    ModifiedRequestedBy = booking.ModifiedRequestedBy, // Or capture from request
                    ModifiedReason = booking.ModifiedReason, // Adjust as necessary
                    ModifiedStatus = booking.ModifiedStatus, // Pending
                    ModifiedBy = (int)this.UserID, // Set the ID of the user making the modification
                    ModifiedByName = this.UserEmail
                };

                _db.BookingModify.Add(bookingModify);

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
                existingBooking.ServiceTime = !string.IsNullOrEmpty(booking.ServiceTime) ? booking.ServiceTime : existingBooking.ServiceTime;
                existingBooking.TripType = !string.IsNullOrEmpty(booking.TripType) ? booking.TripType : existingBooking.TripType;
                existingBooking.DriverPrice = booking.DriverPrice != default ? booking.DriverPrice : existingBooking.DriverPrice;
                existingBooking.VehiclePrice = booking.VehiclePrice != default ? booking.VehiclePrice : existingBooking.VehiclePrice;
                existingBooking.Discount = booking.Discount != default ? booking.Discount : existingBooking.Discount;
                existingBooking.Amount = booking.Amount != default ? booking.Amount : existingBooking.Amount;

                existingBooking.WithFuel = booking.WithFuel;
                existingBooking.Document = !string.IsNullOrEmpty(booking.Document) ? booking.Document : existingBooking.Document;
                existingBooking.CreatedBy = booking.CreatedBy;
                existingBooking.CreatedByName = booking.CreatedByName;
                existingBooking.StatusFlag = booking.StatusFlag;

                _db.Booking.Update(existingBooking);
                _db.SaveChanges();

                return Ok(new { message = "Booking updated successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpPost("cancelBooking/{id}")]
        public IActionResult CancelBooking(int id, [FromBody] BaseModify modify)
        {
            

            var existingBooking = _db.Booking.FirstOrDefault(b => b.BookingId == id);
            if (existingBooking == null)
                return Ok("Booking not found.");

            try
            {
                // Save existing booking data to BookingModify table before updating
                var bookingModify = new BookingModify
                {
                    BookingId = existingBooking.BookingId,
                    VehicleId = existingBooking.VehicleId,
                    BranchId = existingBooking.BranchId,
                    DriverId = existingBooking.DriverId,
                    ClientId = existingBooking.ClientId,
                    ExistingClientId = existingBooking.ExistingClientId,
                    ExistingClientName = existingBooking.ExistingClientName,
                    Date = existingBooking.Date,
                    BookingDateFrom = existingBooking.BookingDateFrom,
                    BookingDateTo = existingBooking.BookingDateTo,
                    Destination = existingBooking.Destination,
                    Arival = existingBooking.Arival,
                    PaymentOption = existingBooking.PaymentOption,
                    WithDriver = existingBooking.WithDriver,
                    DriverPrice = existingBooking.DriverPrice,
                    VehiclePrice = existingBooking.VehiclePrice,
                    Discount = existingBooking.Discount,
                    Amount = existingBooking.Amount,
                    WithFuel = existingBooking.WithFuel,
                    Document = existingBooking.Document,
                    CreatedBy = existingBooking.CreatedBy,
                    CreatedByName = existingBooking.CreatedByName,
                    StatusFlag = existingBooking.StatusFlag,
                    ModifiedRequestedBy = modify.ModifiedRequestedBy, // Or capture from request
                    ModifiedReason = modify.ModifiedReason, // Adjust as necessary
                    ModifiedStatus = modify.ModifiedStatus, // Pending
                    ModifiedBy = (int)this.UserID, // Set the ID of the user making the modification
                    ModifiedByName = this.UserEmail // Set the name that
                };

                _db.BookingModify.Add(bookingModify);

               
                existingBooking.StatusFlag = false;

                _db.Booking.Update(existingBooking);
                _db.SaveChanges();

                return Ok(new { message = "Booking Cancelled successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

        }



    }
}
