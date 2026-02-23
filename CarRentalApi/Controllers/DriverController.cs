using CarRentalApi.Data;
using CarRentalApi.Model;
using CarRentalApi.Service;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update.Internal;
using System.Linq;

namespace CarRentalApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DriverController : BaseController
    {
        private readonly ApplicationDbContext _db;
		
		private readonly IImageUploadService _imageUploadService;
		private readonly ICurrencyConversionService _currencyService;
		public DriverController(ApplicationDbContext db,  IImageUploadService imageUploadService, IHttpContextAccessor contextAccessor, IConfiguration configuration, IWebHostEnvironment hostingEnvironment, ICurrencyConversionService currencyService) : base(hostingEnvironment, contextAccessor, configuration, db)
        {
            _db = db;
            _imageUploadService = imageUploadService;
			_currencyService = currencyService;
			
		}

		[HttpGet]
		public async Task<IActionResult> GetDrivers(
	 [FromQuery] string searchText = "",
	 [FromQuery] string branchIds = "",
	 int currentPageNumber = 1,
	 int pageSize = 50,
	 int orderByColNum = 1,
	 string toCurrency = "USD")
		{
			var query = _db.Driver.AsQueryable();

			// Filter by BranchId
			if (!string.IsNullOrEmpty(branchIds))
			{
				var branchIdList = branchIds.Split(',')
											.Select(int.Parse)
											.ToList();
				query = query.Where(v => branchIdList.Contains(v.BranchId));
			}

			// Search functionality
			if (!string.IsNullOrEmpty(searchText))
			{
				query = query.Where(d => d.DriverName.Contains(searchText) || d.LicensePlate.Contains(searchText));
			}

			// Order by functionality
			switch (orderByColNum)
			{
				case 2:
					query = query.OrderByDescending(d => d.Experience);
					break;
				case 3:
					query = query.OrderByDescending(d => d.Salary);
					break;
				default:
					query = query.OrderBy(d => d.DriverName);
					break;
			}

			// Always order by DriverId in descending order
			query = query.OrderByDescending(d => d.DriverId);

			// Pagination
			int totalItems = query.Count();
			int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
			var drivers = await query.Skip((currentPageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

			// NEW: Fetch branch currencies in bulk
			var branchIdsInDrivers = drivers.Select(d => d.BranchId).Distinct().ToList();
			var branchCurrencies = await _db.LocationMaster
				.Where(b => branchIdsInDrivers.Contains(b.Id))
				.Select(b => new { b.Id, Currency = b.CurrencyCode ?? "USD" }) // Treat null as USD
				.ToDictionaryAsync(b => b.Id, b => b.Currency);

			// NEW: Get conversion rates for unique currencies
			var uniqueCurrencies = branchCurrencies.Values
				.Distinct()
				.Where(c => c != toCurrency) // Filter out same-currency conversions
				.ToList();

			var conversionRates = new Dictionary<string, double>();
			foreach (var currency in uniqueCurrencies)
			{
				conversionRates[currency] = (double)await _currencyService.GetConversionRateAsync(currency, toCurrency);
			}

			// Prepare driver DTOs
			var driverDTOs = new List<DriverDTO>();
			foreach (var driver in drivers)
			{
				// Get documents
				var documents = await _db.DriverDocuments
					.Where(doc => doc.DriverId == driver.DriverId)
					.Select(doc => new DriverADO
					{
						DriverDocumentsId = doc.DriverDocumentsId,
						Name = doc.Name,
						DocumentPath = doc.DocumentPath,
						UploadDate = doc.UploadDate,
						ExpireDate = doc.ExpireDate
					})
					.ToListAsync();

				// NEW: Determine conversion rate
				double rate = 1.0;
				if (branchCurrencies.TryGetValue(driver.BranchId, out var fromCurrency))
				{
					rate = fromCurrency == toCurrency
						? 1.0
						: conversionRates.GetValueOrDefault(fromCurrency, 1.0);
				}

				// Map to DTO
				var driverDTO = new DriverDTO
				{
					DriverId = driver.DriverId,
					DriverName = driver.DriverName,
					Contact = driver.Contact,
					Email = driver.Email,
					LiveStatus = driver.LiveStatus,
					BranchId = driver.BranchId,
					TripsCovered = driver.TripsCovered,
					Address = driver.Address,
					Age = driver.Age,
					Image = driver.Image,
					NationalId = driver.NationalId,
					LicensePlate = driver.LicensePlate,
					Expertise = driver.Expertise,
					PricePerDay = driver.PricePerDay * (decimal)rate, // Converted value
					StatusFlag = driver.StatusFlag,
					Documents = documents,
					Files = null
				};

				driverDTOs.Add(driverDTO);
			}

			var response = new PagedResponse<DriverDTO>
			{
				CurrentPageNumber = currentPageNumber,
				PageSize = pageSize,
				TotalItems = totalItems,
				TotalPages = totalPages,
				Items = driverDTOs
			};

			return Ok(response);
		}



		/* // POST: api/driver
         [HttpPost]
         public IActionResult SetDriver([FromBody] Driver driver)
         {
             // Validate the incoming driver object.
             if (driver == null || !ModelState.IsValid)
             {
                 return BadRequest(ModelState); // Return 400 Bad Request if validation fails.
             }

             // Set default LiveStatus for the driver.
             driver.LiveStatus = "Available";

             // Add the driver to the database context.
             _db.Driver.Add(driver);

             // Save changes to the database, which generates the DriverId.
             _db.SaveChanges();

             // Return a 201 Created response with the generated DriverId in the driver object.
             return CreatedAtAction(nameof(GetDrivers), new { id = driver.DriverId }, driver);
         }*/


		/*[HttpPost("postnew/")]
        public async Task<IActionResult> AddDriverWithDocuments1([FromForm] DriverDTO driver)
        {
            return StatusCode(500, $"Internal server error:");
        }*/

		[HttpPost]
        public async Task<IActionResult> AddDriverWithDocuments([FromForm] DriverDTO driver)
        {
			

			List<DriverADO> documentDetails = driver.Documents;
            

            if (driver == null)
            {
                return Ok("Driver information is missing.");
            }

            try
            {
               
                // Set default LiveStatus for the driver
                driver.LiveStatus = "Available";
                driver.CreatedBy = (int)this.UserID;
                driver.CreatedByName = this.UserEmail;
                // Add the driver to the database
                _db.Driver.Add(driver);

                // Save changes to generate DriverId
                await _db.SaveChangesAsync();

                var savedDocuments = new List<DriverDocuments>();

                // Check if files and documents exist and are not empty

                if (driver.Files != null && documentDetails != null)
                {
                    for (int i = 0; i < documentDetails.Count; i++)
                    {
                        /*var file = driver.Files[i];*/
                        var driverDocument = documentDetails[i];
                        /*DriverDocuments driverDoucmentss ;
                        driverDoucmentss.DriverDocumentsId = 0;
                        driverDoucmentss.DriverId = driver.DriverId;
                        driverDoucmentss.Name = documentDetails[i].Name;*/


                        var driverDoucmentss = new DriverDocuments
                        {
                            DriverDocumentsId = 0,
                            DriverId = driver.DriverId,
                            Name = documentDetails[i].Name,
                            DocumentPath = documentDetails[i].DocumentPath,
                            UploadDate = DateTime.UtcNow,
                            ExpireDate = documentDetails[i].ExpireDate
                        };
                        driverDoucmentss.UploadDate = DateTime.UtcNow;
                        driverDoucmentss.ExpireDate = documentDetails[i].ExpireDate;




                       /* // Upload the file and get its path
                        string filePath = await _imageUploadService.UploadImageAsync(file);

                        // Populate driver document properties*/
                        
                        driverDoucmentss.DriverId = driver.DriverId;

                       

                        // Add the document to the database
                        _db.DriverDocuments.Add(driverDoucmentss);
                        savedDocuments.Add(driverDoucmentss);
                    }

                    // Save all documents to the database
                    await _db.SaveChangesAsync();
                }

                // Return a success response with driver and documents details
                return CreatedAtAction(nameof(GetDrivers), new { id = driver.DriverId }, new
                {
                    message = "Driver added successfully!",
                    driver,
                    documents = savedDocuments
                });
            }
            catch (Exception ex)
            {
                return Ok( $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("update/{id}")]
        public async Task<IActionResult> UpdateDriver(int id, [FromForm] DriverDTO updatedDriver)
        {
			
			if (updatedDriver == null || id != updatedDriver.DriverId)
            {
                return BadRequest("Invalid driver ID.");
            }

            var driver = await _db.Driver.FirstOrDefaultAsync(d => d.DriverId == id);
           
            if (driver == null)
            {
                return Ok($"Driver with ID {id} not found.");
            }

            try
            {
                updatedDriver.PricePerDay = updatedDriver.PricePerDay;
				// Update the driver fields
				driver.DriverName = updatedDriver.DriverName ?? driver.DriverName;
                driver.Contact = updatedDriver.Contact ?? driver.Contact;
                driver.LiveStatus = updatedDriver.LiveStatus ?? driver.LiveStatus;
                driver.Experience = updatedDriver.Experience != 0 ? updatedDriver.Experience : driver.Experience;
                driver.Salary = updatedDriver.Salary;
                driver.LicensePlate = updatedDriver.LicensePlate ?? driver.LicensePlate;
                driver.Address = updatedDriver.Address ?? driver.Address;
                driver.Age = updatedDriver.Age != 0 ? updatedDriver.Age : driver.Age;
                driver.Email = updatedDriver.Email ?? driver.Email;
                driver.NationalId = updatedDriver.NationalId ?? driver.NationalId;
                driver.EmergencyNumber = updatedDriver.EmergencyNumber ?? driver.EmergencyNumber;
                driver.Image = updatedDriver.Image ?? driver.Image;
                driver.Expertise = updatedDriver.Expertise ?? driver.Expertise;
                driver.VerificationDocument = updatedDriver.VerificationDocument ?? driver.VerificationDocument;
                driver.VerificationStatus = updatedDriver.VerificationStatus;
                driver.PricePerDay = updatedDriver.PricePerDay;
                driver.LicenseExp = updatedDriver.LicenseExp ?? driver.LicenseExp;

                _db.Driver.Update(driver);
                await _db.SaveChangesAsync();

                if (updatedDriver.Documents != null )
                {
                    for (int i = 0; i < updatedDriver.Documents.Count; i++)
                    {
                        var doc = updatedDriver.Documents[i];
                        

                        if (doc.DriverDocumentsId > 0)
                        {
                            // Update existing document
                            var existingDocument = await _db.DriverDocuments
                                .FirstOrDefaultAsync(d => d.DriverDocumentsId == doc.DriverDocumentsId && d.DriverId == id);

                            if (existingDocument != null)
                            {
                                existingDocument.Name = doc.Name;
                                existingDocument.ExpireDate = doc.ExpireDate;
                                existingDocument.DriverId = updatedDriver.DriverId;
                                existingDocument.DocumentPath = doc.DocumentPath;
                                existingDocument.UploadDate = DateTime.UtcNow;

                               

                                _db.DriverDocuments.Update(existingDocument);
                            }
                        }
                        else
                        {
                            // Add new document
                            var newDocument = new DriverDocuments
                            {
                                DriverId = updatedDriver.DriverId,
                                Name = doc.Name,
                                ExpireDate = doc.ExpireDate,
                                UploadDate = DateTime.UtcNow,
                                DocumentPath = doc.DocumentPath,
                            };

                           

                            _db.DriverDocuments.Add(newDocument);
                        }
                    }

                    await _db.SaveChangesAsync();
                }

                return Ok(new { message = "Driver and documents updated successfully!" });
            }
            catch (Exception ex)
            {
                return Ok($"Internal server error: {ex.Message}");
            }
        }





        [HttpGet("{driverId}")]
        public async Task<IActionResult> GetDriverById(int driverId, string toCurrency = "USD")
		{
			decimal conversionRate = await _currencyService.GetConversionRateAsync("USD", toCurrency);
			try
            {
                // Fetch the driver by ID from the Driver table
                var driver = await _db.Driver
                    .Where(d => d.DriverId == driverId)
                    .FirstOrDefaultAsync();

                if (driver == null)
                {
                    return Ok($"No driver found with ID {driverId}.");
                }

                // Get the associated documents for the driver
                var documents = await _db.DriverDocuments
                    .Where(doc => doc.DriverId == driverId)
                    .Select(doc => new DriverADO  // Mapping the result to DriverADO
                    {
                        DriverDocumentsId = doc.DriverDocumentsId,
                        Name = doc.Name,
                        DocumentPath = doc.DocumentPath,
                        UploadDate = doc.UploadDate,
                        ExpireDate = doc.ExpireDate
                    })
                    .ToListAsync();

                // Map the driver and documents data to the DTO
                var driverDTO = new DriverDTO
                {
                    DriverId = driver.DriverId,
                    DriverName = driver.DriverName,
                    Contact = driver.Contact,
                    Email = driver.Email,
                    LiveStatus = driver.LiveStatus,
                    BranchId = driver.BranchId,
                    TripsCovered = driver.TripsCovered,
                    NationalId = driver.NationalId,
                    Address = driver.Address,
                    Age = driver.Age,
                    EmergencyNumber = driver.EmergencyNumber,
                    Salary= driver.Salary,
                    Experience = driver.Experience,
                    LicensePlate = driver.LicensePlate,
                    Expertise = driver.Expertise,
                    Image = driver.Image,
                    PricePerDay = driver.PricePerDay * conversionRate,
                    VerificationStatus = driver.VerificationStatus,
                    VerificationDocument = driver.VerificationDocument,
                    LicenseExp = driver.LicenseExp,
                    StatusFlag = driver.StatusFlag,

                    Documents = documents,  // Adding the list of documents
                    Files = null  // Set to null if you're not handling file uploads here
                };

                return Ok(driverDTO);
            }
            catch (Exception ex)
            {
                return Ok($"Internal server error: {ex.Message}");
            }
        }



		[HttpGet("available")]
		public async Task<IActionResult> GetAvailableDrivers(
	[FromQuery] DateTime startDate,
	[FromQuery] DateTime endDate,
	[FromQuery] int branchId = 0,
	[FromQuery] string toCurrency = "USD")
		{
			var query = _db.Driver.AsQueryable().Where(d => d.StatusFlag == true);

			// Filter by branch
			if (branchId > 0)
			{
				query = query.Where(d => d.BranchId == branchId);
			}

			// 1. Get all bookings in date range with assigned driver
			var overlappingBookings = _db.Booking
				.Where(b => b.StatusFlag == true &&
						   b.DriverId != null &&
						   b.BookingDateFrom <= endDate &&
						   b.BookingDateTo >= startDate)
				.Select(b => new { b.BookingId, b.DriverId });

			// 2. Get bookings with completed checklist
			var completedChecklistBookingIds = _db.CheckList
				.Where(cl => cl.OdometerAfter > 0)
				.Select(cl => cl.BookingId)
				.Distinct();

			// 3. Only exclude drivers where booking exists in range AND checklist is incomplete/missing
			var bookedDriverIds = await overlappingBookings
				.Where(b => !completedChecklistBookingIds.Contains(b.BookingId))
				.Select(b => b.DriverId.Value)
				.Distinct()
				.ToListAsync();

			// 4. Exclude drivers on leave
			var driverLeaveIds = await _db.DriverLeave
				.Where(l => l.LeaveDateFrom <= endDate && l.LeaveDateTo >= startDate)
				.Select(l => l.DriverId)
				.Distinct()
				.ToListAsync();

			// 5. Final filter
			query = query.Where(d =>
				!bookedDriverIds.Contains(d.DriverId) &&
				!driverLeaveIds.Contains(d.DriverId)
			);

			// NEW: Get branch currencies for all available drivers
			var availableDriverIds = await query.Select(d => d.BranchId).Distinct().ToListAsync();
			var branchCurrencies = await _db.LocationMaster
				.Where(b => availableDriverIds.Contains(b.Id))
				.Select(b => new { b.Id, Currency = b.CurrencyCode ?? "USD" })
				.ToDictionaryAsync(b => b.Id, b => b.Currency);

			// NEW: Get conversion rates for unique currencies
			var uniqueCurrencies = branchCurrencies.Values
				.Distinct()
				.Where(c => c != toCurrency)
				.ToList();

			var conversionRates = new Dictionary<string, decimal>();
			foreach (var currency in uniqueCurrencies)
			{
				conversionRates[currency] = await _currencyService.GetConversionRateAsync(currency, toCurrency);
			}

			// 6. Final result with per-driver currency adjustment
			var availableDrivers = await query
				.OrderByDescending(d => d.DriverId)
				.Select(driver => new
				{
					driver.DriverId,
					driver.DriverName,
					driver.Contact,
					driver.Email,
					driver.LiveStatus,
					driver.BranchId,
					driver.TripsCovered,
					driver.NationalId,
					driver.Address,
					driver.Age,
					driver.EmergencyNumber,
					driver.Salary,
					driver.Experience,
					driver.LicensePlate,
					driver.Expertise,
					driver.Image,
					OriginalPricePerDay = driver.PricePerDay,
					BranchCurrency = branchCurrencies.ContainsKey(driver.BranchId)
						? branchCurrencies[driver.BranchId]
						: "USD",
					driver.VerificationStatus,
					driver.VerificationDocument,
					driver.LicenseExp,
					driver.StatusFlag
				})
				.ToListAsync();

			// Apply currency conversion
			var result = availableDrivers.Select(driver =>
			{
				var rate = driver.BranchCurrency == toCurrency
					? 1.0m
					: conversionRates.GetValueOrDefault(driver.BranchCurrency, 1.0m);

				return new
				{
					driver.DriverId,
					driver.DriverName,
					driver.Contact,
					driver.Email,
					driver.LiveStatus,
					driver.BranchId,
					driver.TripsCovered,
					driver.NationalId,
					driver.Address,
					driver.Age,
					driver.EmergencyNumber,
					driver.Salary,
					driver.Experience,
					driver.LicensePlate,
					driver.Expertise,
					driver.Image,
					PricePerDay = driver.OriginalPricePerDay * rate,
					OriginalPricePerDay = driver.OriginalPricePerDay,
					OriginalCurrency = driver.BranchCurrency,
					TargetCurrency = toCurrency,
					driver.VerificationStatus,
					driver.VerificationDocument,
					driver.LicenseExp,
					driver.StatusFlag
				};
			}).ToList();

			return result.Count == 0
				? Ok(new { message = "No available drivers found for the given date range." })
				: Ok(result);
		}
		// DELETE: api/driver/{id}
		[HttpPost("delete/{id}")]
        public IActionResult DeleteDriver(int id)
        {
            var driver = _db.Driver.FirstOrDefault(d => d.DriverId == id);

            if (driver == null)
            {
                return Ok(new { message = "Driver not found." });
            }

            try
            {
                _db.Driver.Remove(driver);
                _db.SaveChanges();
                return Ok(new { message = "Driver deleted successfully!" });
            }
            catch (Exception ex)
            {
                return Ok($"Internal server error: {ex.Message}");
            }
        }
        [HttpPost("ChangeStatus/{id}")]
        public IActionResult ChangeStatus(int id)
        {
            var driver = _db.Driver.FirstOrDefault(d => d.DriverId == id);

            if (driver == null)
            {
                return Ok(new { message = "Driver not found." });
            }

            try
            {
                if(driver.StatusFlag)
                {
                    driver.StatusFlag = false;
                }
                else
                {
                    driver.StatusFlag = true;
                }
                _db.Driver.Update(driver);
                _db.SaveChanges();
                return Ok(new { message = "Driver Status Changed successfully!" });
            }
            catch (Exception ex)
            {
                return Ok($"Internal server error: {ex.Message}");
            }
        }
		/*// GET: api/driver/all-details
        [HttpGet("all-details")]
        public IActionResult GetAllDriverDetails(int? branchId = null)
        {
            var query = _db.Driver.AsQueryable();

            // Apply BranchId filter if provided
            if (branchId.HasValue)
            {
                query = query.Where(d => d.BranchId == branchId.Value);
            }

            var drivers = query
                .Select(d => new
                {
                    d.DriverId,
                    d.DriverName,
                    d.Image,
                    d.Contact,
                    d.LicenseExp,
                    d.NationalId,
                    d.PricePerDay,
                    LeaveCount = _db.DriverLeave.Count(dl => dl.DriverId == d.DriverId),
                    d.Salary
                })
                .OrderByDescending(d => d.DriverId)
                .ToList();

            if (!drivers.Any())
            {
                return Ok(new { message = "No drivers found." });
            }

            return Ok(drivers);
        }
*/
		[HttpGet("all-details")]
		public async Task<IActionResult> GetAllDriverDetails(
	 [FromQuery] string branchIds = "",
	 [FromQuery] int currentPageNumber = 1,
	 [FromQuery] int pageSize = 50,
	 [FromQuery] string? searchText = null,
	 [FromQuery] int branchId = 0,
	 [FromQuery] string toCurrency = "USD")
		{
			if (currentPageNumber <= 0 || pageSize <= 0)
			{
				return BadRequest("Current page number and page size must be greater than 0.");
			}

			var query = _db.Driver.AsQueryable();

			// Apply BranchId filter if provided
			if (!string.IsNullOrEmpty(branchIds))
			{
				var branchIdList = branchIds.Split(',')
										   .Select(int.Parse)
										   .ToList();
				query = query.Where(v => branchIdList.Contains(v.BranchId));
			}
			else if (branchId > 0)
			{
				query = query.Where(v => v.BranchId == branchId);
			}

			// Apply search filter if provided
			if (!string.IsNullOrWhiteSpace(searchText))
			{
				query = query.Where(d => d.DriverName.Contains(searchText) || d.Contact.Contains(searchText));
			}

			// NEW: Get branch currencies for all matching drivers
			var branchIdsInResults = await query.Select(d => d.BranchId).Distinct().ToListAsync();
			var branchCurrencies = await _db.LocationMaster
				.Where(b => branchIdsInResults.Contains(b.Id))
				.Select(b => new { b.Id, Currency = b.CurrencyCode ?? "USD" })
				.ToDictionaryAsync(b => b.Id, b => b.Currency);

			// NEW: Get conversion rates for unique currencies
			var uniqueCurrencies = branchCurrencies.Values
				.Distinct()
				.Where(c => c != toCurrency)
				.ToList();

			var conversionRates = new Dictionary<string, decimal>();
			foreach (var currency in uniqueCurrencies)
			{
				conversionRates[currency] = await _currencyService.GetConversionRateAsync(currency, toCurrency);
			}

			// Get paginated results
			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var paginatedDrivers = await query
				.OrderByDescending(d => d.DriverId)
				.Skip((currentPageNumber - 1) * pageSize)
				.Take(pageSize)
				.Select(d => new
				{
					d.DriverId,
					d.DriverName,
					d.Image,
					d.Contact,
					d.LicenseExp,
					d.NationalId,
					d.StatusFlag,
					OriginalPricePerDay = d.PricePerDay,
					BranchId = d.BranchId,
					d.Salary
				})
				.ToListAsync();

			// Get all driver IDs in the current page
			var driverIds = paginatedDrivers.Select(d => d.DriverId).ToList();

			// Fetch driver leaves
			var leaves = await _db.DriverLeave
				.Where(dl => driverIds.Contains(dl.DriverId))
				.ToListAsync();

			// Prepare final response with currency conversion
			var driverDetailsWithLeave = paginatedDrivers.Select(d =>
			{
				var rate = branchCurrencies.TryGetValue(d.BranchId, out var fromCurrency)
					? (fromCurrency == toCurrency ? 1.0m : conversionRates.GetValueOrDefault(fromCurrency, 1.0m))
					: 1.0m;

				return new
				{
					d.DriverId,
					d.DriverName,
					d.Image,
					d.Contact,
					d.LicenseExp,
					d.NationalId,
					d.StatusFlag,
					PricePerDay = d.OriginalPricePerDay * rate,
					OriginalPrice = d.OriginalPricePerDay,
					OriginalCurrency = branchCurrencies.GetValueOrDefault(d.BranchId, "USD"),
					TargetCurrency = toCurrency,
					d.Salary,
					LeaveCount = leaves
						.Where(dl => dl.DriverId == d.DriverId)
						.Sum(dl => (dl.LeaveDateTo - dl.LeaveDateFrom).Days + 1)
				};
			}).ToList();

			var response = new
			{
				CurrentPageNumber = currentPageNumber,
				PageSize = pageSize,
				TotalItems = totalItems,
				TotalPages = totalPages,
				Items = driverDetailsWithLeave
			};

			return Ok(response);
		}


		[HttpPost("document/update/{id}")]
        public async Task<IActionResult> UpdateDriverDocument(int id, [FromForm] DriverDocuments updatedDriverDocument, IFormFile file)
        {
            var driverDocument = await _db.DriverDocuments.FindAsync(id);
            if (driverDocument == null)
            {
                return Ok(new { message = "Driver document not found." });
            }

            try
            {
                // Update driver document properties
                driverDocument.Name = updatedDriverDocument.Name ?? driverDocument.Name;
                driverDocument.DriverId = updatedDriverDocument.DriverId ?? driverDocument.DriverId;
                driverDocument.ExpireDate = updatedDriverDocument.ExpireDate ?? driverDocument.ExpireDate;

                // If a new file is provided, upload and update the DocumentPath
                if (file != null)
                {
                   /* // Delete old file if it exists
                    if (!string.IsNullOrEmpty(driverDocument.DocumentPath))
                    {
                        DeleteFile(driverDocument.DocumentPath);
                    }
*/
                    // Save new file and update DocumentPath
                    string newFilePath = await _imageUploadService.UploadImageAsync(file);
                    driverDocument.DocumentPath = newFilePath;
                }

                // Update driver document in the database
                _db.DriverDocuments.Update(driverDocument);
                await _db.SaveChangesAsync();

                return Ok(new { message = "Driver document updated successfully!" });
            }
            catch (Exception ex)
            {
                return Ok($"Internal server error: {ex.Message}");
            }
        }


        [HttpGet("document/driver/{driverId}")]
        public IActionResult GetDocumentsByDriverId(int driverId)
        {
            var driverDocuments = _db.DriverDocuments.Where(dd => dd.DriverId == driverId).ToList();
            if (!driverDocuments.Any())
            {
                return Ok(new { message = "No documents found for this driver." });
            }

            return Ok(driverDocuments);
        }

        [HttpPost("document/delete/{id}")]
        public async Task<IActionResult> DeleteDriverDocument(int id)
        {
            var driverDocument = await _db.DriverDocuments.FindAsync(id);
            if (driverDocument == null)
            {
                return Ok("Driver document not found.");
            }

            try
            {/*
                // Delete file from storage
                DeleteFile(driverDocument.DocumentPath);*/

                // Remove driver document from the database
                _db.DriverDocuments.Remove(driverDocument);
                await _db.SaveChangesAsync();

                return Ok(new { message = "Driver document deleted successfully!" });
            }
            catch (Exception ex)
            {
                return Ok($"Internal server error: {ex.Message}");
            }
        }
		[HttpGet("availableStatus")]
		public async Task<IActionResult> GetDriversWithAvailability(
	 [FromQuery] DateTime startDate,
	 [FromQuery] DateTime endDate,
	 [FromQuery] int branchId = 0,
	 [FromQuery] string toCurrency = "USD")
		{
			// Get all drivers, optionally filter by branch
			var driversQuery = _db.Driver.AsQueryable();
			if (branchId > 0)
			{
				driversQuery = driversQuery.Where(d => d.BranchId == branchId);
			}

			// NEW: Get branch currencies for all matching drivers
			var branchIdsInResults = await driversQuery.Select(d => d.BranchId).Distinct().ToListAsync();
			var branchCurrencies = await _db.LocationMaster
				.Where(b => branchIdsInResults.Contains(b.Id))
				.Select(b => new { b.Id, Currency = b.CurrencyCode ?? "USD" })
				.ToDictionaryAsync(b => b.Id, b => b.Currency);

			// NEW: Get conversion rates for unique currencies
			var uniqueCurrencies = branchCurrencies.Values
				.Distinct()
				.Where(c => c != toCurrency)
				.ToList();

			var conversionRates = new Dictionary<string, decimal>();
			foreach (var currency in uniqueCurrencies)
			{
				conversionRates[currency] = await _currencyService.GetConversionRateAsync(currency, toCurrency);
			}

			// Get all driver IDs that are booked within the given date range
			var bookedDriverIds = await _db.Booking
				.Where(b => b.BookingDateFrom <= endDate && b.BookingDateTo >= startDate)
				.Select(b => b.DriverId)
				.Distinct()
				.ToListAsync();

			// Convert to HashSet for efficient lookup
			var bookedDriverIdsHash = new HashSet<int?>(bookedDriverIds);

			// Get all drivers with currency information
			var allDrivers = await driversQuery
				.Select(d => new
				{
					d.DriverId,
					d.DriverName,
					d.LiveStatus,
					d.BranchId,
					d.TripsCovered,
					d.Experience,
					d.Contact,
					d.Email,
					d.Address,
					d.Age,
					d.StatusFlag,
					d.LicensePlate,
					d.Salary,
					d.NationalId,
					d.EmergencyNumber,
					d.Image,
					d.VerificationDocument,
					d.VerificationStatus,
					d.Expertise,
					OriginalPricePerDay = d.PricePerDay,
					d.CreatedBy,
					d.CreatedByName,
					d.LicenseExp
				})
				.ToListAsync();

			// Map all drivers to a new object with availability status and converted prices
			var driversWithAvailability = allDrivers.Select(d =>
			{
				var rate = branchCurrencies.TryGetValue(d.BranchId, out var fromCurrency)
					? (fromCurrency == toCurrency ? 1.0m : conversionRates.GetValueOrDefault(fromCurrency, 1.0m))
					: 1.0m;

				return new
				{
					d.DriverId,
					d.DriverName,
					d.LiveStatus,
					d.BranchId,
					d.TripsCovered,
					d.Experience,
					d.Contact,
					d.Email,
					d.Address,
					d.Age,
					d.StatusFlag,
					d.LicensePlate,
					d.Salary,
					d.NationalId,
					d.EmergencyNumber,
					d.Image,
					d.VerificationDocument,
					d.VerificationStatus,
					d.Expertise,
					PricePerDay = d.OriginalPricePerDay * rate,
					OriginalPrice = d.OriginalPricePerDay,
					OriginalCurrency = branchCurrencies.GetValueOrDefault(d.BranchId, "USD"),
					TargetCurrency = toCurrency,
					d.CreatedBy,
					d.CreatedByName,
					d.LicenseExp,
					IsAvailable = d.LiveStatus == "Available" && !bookedDriverIdsHash.Contains(d.DriverId)
				};
			})
			.OrderByDescending(d => d.DriverId)
			.ToList();

			return Ok(driversWithAvailability);
		}


		/*private void DeleteFile(string filePath)
        {
            var fullPath = Path.Combine(_imageUploadService.RootPath, filePath);
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }*/

	}
}
