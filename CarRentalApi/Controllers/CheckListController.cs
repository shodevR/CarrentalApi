using CarRentalApi.Data;
using CarRentalApi.Model;
using CarRentalApi.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace CarRentalApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CheckListController :BaseController
    {
		
		private readonly ApplicationDbContext _db;
		private readonly ICurrencyConversionService _currencyService;

		public CheckListController(ApplicationDbContext db
		  , IHttpContextAccessor contextAccessor, IConfiguration configuration, IWebHostEnvironment hostingEnvironment, ICurrencyConversionService currencyService)
			  : base(hostingEnvironment, contextAccessor, configuration, db)
		{
			_db = db;
			_currencyService = currencyService;
			

		}

		// POST: Store the 'before' checklist record and set Vehicle and Driver status to 'OnTrip'
		[HttpPost("before")]
        public IActionResult SaveCheckListBefore([FromBody] CheckList checkList)
        {
            if (checkList == null)
            {
                return BadRequest("Invalid CheckList data.");
            }

            try
            {
                checkList.CreatedByBefore = (int)this.UserID;
                // Store 'before' record
                _db.CheckList.Add(checkList);

                // Set vehicle status to 'OnTrip'
                var vehicle = _db.Vehicle.FirstOrDefault(v => v.VehicleId == checkList.VehicleId);
                if (vehicle != null)
                {
                    vehicle.LiveStatus = "OnTrip";
                    _db.Vehicle.Update(vehicle);
                }

                // Set driver status to 'OnTrip'
                var booking = _db.Booking.FirstOrDefault(b => b.BookingId == checkList.BookingId);
                if (booking != null)
                {
                    var driver = _db.Driver.FirstOrDefault(d => d.DriverId == booking.DriverId);
                    if (driver != null)
                    {
                        driver.LiveStatus = "OnTrip";
                        _db.Driver.Update(driver);
                    }
                }

                _db.SaveChanges();

                return Ok(new { message = "CheckList 'before' record created successfully!", checkListId = checkList.CheckListId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("after/{bookingId}")]
        public IActionResult UpdateCheckListAfter(int bookingId, [FromBody] CheckList checkList)
        {
            if (checkList == null || checkList.BookingId != bookingId)
            {
                return BadRequest("Invalid CheckList data or Booking ID mismatch.");
            }

            // Fetch the latest checklist for the given BookingId
            var existingCheckList = _db.CheckList
                .Where(cl => cl.BookingId == bookingId)
                .OrderByDescending(cl => cl.CheckListId)
                .FirstOrDefault();
           
           

			if (existingCheckList != null)
            {
                 if(existingCheckList.OdometerBefore >= checkList.OdometerAfter)
                            {
                                return Ok("Odometer reading cannot be less or equal to the Before Data.");
                            }
            }
           
            if (existingCheckList == null)
            {
                return Ok("CheckList not found.");
            }

            try
            {
                // Update 'after' record fields
                existingCheckList.OdometerAfter = checkList.OdometerAfter;
                existingCheckList.FuelAfter = checkList.FuelAfter;
                existingCheckList.TyrePressureAfter = checkList.TyrePressureAfter;
                existingCheckList.ExtraHours = checkList.ExtraHours;
                existingCheckList.CarToolsAfter = checkList.CarToolsAfter;
                existingCheckList.SafetyEquipmentsAfter = checkList.SafetyEquipmentsAfter;
                existingCheckList.DateOfReadingAfter = checkList.DateOfReadingAfter;
                existingCheckList.CreatedByAfter = (int)this.UserID; // Update CreatedByAfter field with current user ID

                _db.CheckList.Update(existingCheckList);

                // Get the vehicle associated with the checklist
                var vehicle = _db.Vehicle.FirstOrDefault(v => v.VehicleId == existingCheckList.VehicleId);
                if (vehicle != null)
                {
                    vehicle.LiveStatus = "Available";
                    vehicle.TotalTrips += 1; // Increment TotalTrips by 1
                    _db.Vehicle.Update(vehicle);
                }

                // Get the driver associated with the booking
                var booking = _db.Booking.FirstOrDefault(b => b.BookingId == bookingId);
                if (booking != null)
                {
                    var driver = _db.Driver.FirstOrDefault(d => d.DriverId == booking.DriverId);
                    if (driver != null)
                    {
                        driver.LiveStatus = "Available";
                        driver.TripsCovered += 1; // Increment TripsCovered by 1
                        _db.Driver.Update(driver);
                    }
                }

				var priceMaster = _db.PriceMaster.Where(cl => cl.VehicleId == existingCheckList.VehicleId).OrderByDescending(cl => cl.PriceMasterId).FirstOrDefault();
				var ExistingBooking = _db.Booking.Where(b => b.BookingId == bookingId).OrderByDescending(b => b.BookingId).FirstOrDefault();
				if (existingCheckList != null && existingCheckList.VehicleId > 0 && checkList.ExtraHours > 0)
				{
					
                    double charges = 1;
					double extraCharges=1;

					if (ExistingBooking != null && priceMaster != null)
                    {
                        if (ExistingBooking.WithFuel == true)
                        {
                            if (ExistingBooking.TripType == "inCity") 
                            {
                                charges = (double)priceMaster.WithinCityExtraHoursCharges;
                                extraCharges = charges * (double)checkList.ExtraHours;
                            } 
                            else if (ExistingBooking.TripType == "outCity")
							{
								charges = (double)priceMaster.OutsideCityExtraHoursCharges;
								extraCharges = charges * (double)checkList.ExtraHours;

							}
                           
                        } else if (ExistingBooking.WithFuel == false) {
							if (ExistingBooking.TripType == "inCity")
							{
								charges = (double)priceMaster.WithoutFuelWithinExtraHoursCharges;
								extraCharges = charges * (double)checkList.ExtraHours;
							}
							else if (ExistingBooking.TripType == "outCity")
							{
								charges = (double)priceMaster.WithoutFuelOutsideExtraHoursCharges;
								extraCharges = charges * (double)checkList.ExtraHours;

							}

						}


					}
					else if (ExistingBooking.TripType == "inCity" && ExistingBooking.WithFuel == true)
					{
						charges = (double)priceMaster.WithoutFuelWithinExtraHoursCharges;
						extraCharges = charges * (double)checkList.ExtraHours;

					}

                    ExistingBooking.ExtraCharges = (decimal)extraCharges;
                    ExistingBooking.Amount = ExistingBooking.Amount + (decimal)extraCharges;
					_db.Booking.Update(ExistingBooking);
                }
                    
                   


				
				// Save all changes
				_db.SaveChanges();

                return Ok(new { message = "CheckList 'after' record updated successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }



        [HttpGet]
        public IActionResult GetCheckLists([FromQuery] int vehicleId = 0, int bookingId = 0, [FromQuery] string branchIds = "", int currentPageNumber = 1, int pageSize = 50)
        {
            var query = _db.CheckList.AsQueryable();

            // Filter by VehicleId
            if (vehicleId > 0)
            {
                query = query.Where(cl => cl.VehicleId == vehicleId);
            }

            // Filter by BookingId
            if (bookingId > 0)
            {
                query = query.Where(cl => cl.BookingId == bookingId);
            }

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


            // Include the vehicle information (VehicleName, VehicleNumber) in the query
            var checkListWithVehicle = query
                .Join(_db.Vehicle,
                      cl => cl.VehicleId,
                      v => v.VehicleId,
                      (cl, v) => new
                      {
                          CheckList = cl,
                          VehicleName = v.VehicleName,
                          VehicleNumber = v.VehicleNumber,
                          VehicleId = v.VehicleId
                      })
                .OrderByDescending(clv => clv.CheckList.BookingId);

            // Pagination
            int totalItems = checkListWithVehicle.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var checkLists = checkListWithVehicle
                .Skip((currentPageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList()
                .Select(clv => new CheckListWithVehicleResponse
                {
                    CheckListId = clv.CheckList.CheckListId,
                    BookingId = clv.CheckList.BookingId,
                    BranchId = clv.CheckList.BranchId,
                    VehicleId = clv.CheckList.VehicleId,
                    OdometerBefore = clv.CheckList.OdometerBefore,
                    OdometerAfter = clv.CheckList.OdometerAfter,
                    FuelBefore = clv.CheckList.FuelBefore,
                    FuelAfter = clv.CheckList.FuelAfter,
                    TyrePressureBefore = clv.CheckList.TyrePressureBefore,
                    TyrePressureAfter = clv.CheckList.TyrePressureAfter,
                    CarToolsBefore = clv.CheckList.CarToolsBefore,
                    CarToolsAfter = clv.CheckList.CarToolsAfter,
                    SafetyEquipmentsBefore = clv.CheckList.SafetyEquipmentsBefore,
                    SafetyEquipmentsAfter = clv.CheckList.SafetyEquipmentsAfter,
                    DateOfReadingBefore = clv.CheckList.DateOfReadingBefore,
                    DateOfReadingAfter = clv.CheckList.DateOfReadingAfter,
                    CreatedByBefore = clv.CheckList.CreatedByBefore,
                    CreatedByAfter = clv.CheckList.CreatedByAfter,
                    StatusFlag = clv.CheckList.StatusFlag,
                    ExtraHours= clv.CheckList.ExtraHours,
                    VehicleName = clv.VehicleName,
                    VehicleNumber = clv.VehicleNumber,
                })
                .ToList();

            var response = new PagedResponse<CheckListWithVehicleResponse>
            {
                CurrentPageNumber = currentPageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Items = checkLists
            };

            return Ok(response);
        }


        // Define the response model that matches your desired format
        public class CheckListWithVehicleResponse : CheckList
        {
            public string VehicleName { get; set; }
            public string VehicleNumber { get; set; }
        }


        [HttpGet("{id}")]
        public IActionResult GetCheckListById(int id)
        {
            var checkList = _db.CheckList.FirstOrDefault(cl => cl.CheckListId == id);

            if (checkList == null)
            {
                return Ok(new { message = "CheckList not found." });
            }

            return Ok(checkList);
        }
        [HttpPost]
        public IActionResult SaveCheckList([FromBody] CheckList checkList)
        {
            if (checkList == null)
            {
                return BadRequest("Invalid CheckList data.");
            }

            try
            {
                _db.CheckList.Add(checkList);
                _db.SaveChanges();

                return Ok(new { message = "CheckList created successfully!", checkListId = checkList.CheckListId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpPost("update/{id}")]
        public IActionResult UpdateCheckList(int id, [FromBody] CheckList checkList)
        {
            if (checkList == null || checkList.CheckListId != id)
            {
                return BadRequest("Invalid CheckList data or ID mismatch.");
            }

            var existingCheckList = _db.CheckList.FirstOrDefault(cl => cl.CheckListId == id);

            if (existingCheckList == null)
            {
                return Ok("CheckList not found.");
            }

            try
            {
                // Update properties
                existingCheckList.BookingId = checkList.BookingId;
                existingCheckList.VehicleId = checkList.VehicleId;
                existingCheckList.OdometerBefore = checkList.OdometerBefore;
                existingCheckList.OdometerAfter = checkList.OdometerAfter;
                existingCheckList.BranchId = checkList.BranchId;
                existingCheckList.FuelBefore = checkList.FuelBefore;
                existingCheckList.FuelAfter = checkList.FuelAfter;
                existingCheckList.TyrePressureBefore = checkList.TyrePressureBefore;
                existingCheckList.TyrePressureAfter = checkList.TyrePressureAfter;
                existingCheckList.CarToolsBefore = checkList.CarToolsBefore;
                existingCheckList.CarToolsAfter = checkList.CarToolsAfter;
                existingCheckList.SafetyEquipmentsBefore = checkList.SafetyEquipmentsBefore;
                existingCheckList.SafetyEquipmentsAfter = checkList.SafetyEquipmentsAfter;
                existingCheckList.DateOfReadingBefore = checkList.DateOfReadingBefore;
                existingCheckList.DateOfReadingAfter = checkList.DateOfReadingAfter;
                existingCheckList.CreatedByBefore = checkList.CreatedByBefore ;  
                existingCheckList.CreatedByAfter = (int)this.UserID;

                if(existingCheckList.ExtraHours != checkList.ExtraHours)
                {


					var priceMaster = _db.PriceMaster.Where(cl => cl.VehicleId == existingCheckList.VehicleId).OrderByDescending(cl => cl.PriceMasterId).FirstOrDefault();
					var ExistingBooking = _db.Booking.Where(b => b.BookingId == existingCheckList.BookingId).OrderByDescending(b => b.BookingId).FirstOrDefault();
					if (existingCheckList != null && existingCheckList.VehicleId > 0 && checkList.ExtraHours > 0)
					{

						double charges = 1;
						double extraCharges = 1;

						if (ExistingBooking != null && priceMaster != null)
						{
							if (ExistingBooking.WithFuel == true)
							{
								if (ExistingBooking.TripType == "inCity")
								{
									charges = (double)priceMaster.WithinCityExtraHoursCharges;
									extraCharges = charges * (double)checkList.ExtraHours;
								}
								else if (ExistingBooking.TripType == "outCity")
								{
									charges = (double)priceMaster.OutsideCityExtraHoursCharges;
									extraCharges = charges * (double)checkList.ExtraHours;

								}

							}
							else if (ExistingBooking.WithFuel == false)
							{
								if (ExistingBooking.TripType == "inCity")
								{
									charges = (double)priceMaster.WithoutFuelWithinExtraHoursCharges;
									extraCharges = charges * (double)checkList.ExtraHours;
								}
								else if (ExistingBooking.TripType == "outCity")
								{
									charges = (double)priceMaster.WithoutFuelOutsideExtraHoursCharges;
									extraCharges = charges * (double)checkList.ExtraHours;

								}

							}


						}
						else if (ExistingBooking.TripType == "inCity" && ExistingBooking.WithFuel == true)
						{
							charges = (double)priceMaster.WithoutFuelWithinExtraHoursCharges;
							extraCharges = charges * (double)checkList.ExtraHours;

						}
						ExistingBooking.Amount = ExistingBooking.Amount - ExistingBooking.ExtraCharges;
						ExistingBooking.ExtraCharges = (decimal)extraCharges;
						ExistingBooking.Amount = ExistingBooking.Amount + (decimal)extraCharges;
						_db.Booking.Update(ExistingBooking);
					}




				}


				_db.CheckList.Update(existingCheckList);
                _db.SaveChanges();

                return Ok(new { message = "CheckList updated successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpPost("delete/{id}")]
        public IActionResult DeleteCheckList(int id)
        {
            var checkList = _db.CheckList.FirstOrDefault(cl => cl.CheckListId == id);

            if (checkList == null)
            {
                return Ok(new { message = "CheckList not found." });
            }

            try
            {
                _db.CheckList.Remove(checkList);
                _db.SaveChanges();

                return Ok(new { message = "CheckList deleted successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpGet("vehicle/{vehicleId}")]
        public IActionResult GetCheckListsByVehicleId(int vehicleId)
        {
            var checkLists = _db.CheckList.Where(cl => cl.VehicleId == vehicleId).OrderByDescending(c => c.CheckListId).ToList();


            if (!checkLists.Any())
            {
                return Ok(new { message = $"No checklists found for Vehicle ID {vehicleId}." });
            }

            return Ok(checkLists);
        }
        [HttpGet("booking/{bookingId}")]
        public IActionResult GetCheckListsByBookingId(int bookingId)
        {
            var checkLists = _db.CheckList.Where(cl => cl.BookingId == bookingId).OrderByDescending(c=>c.CheckListId).ToList();

            if (!checkLists.Any())
            {
                return Ok(new { message = $"No checklists found for Booking ID {bookingId}." });
            }

            return Ok(checkLists);
        }



    }
}
