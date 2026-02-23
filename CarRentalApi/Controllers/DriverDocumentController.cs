using CarRentalApi.Data;
using CarRentalApi.Model;
using CarRentalApi.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CarRentalApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DriverDocumentController :BaseController
    {
		
		private readonly ApplicationDbContext _db;
		private readonly ICurrencyConversionService _currencyService;

		public DriverDocumentController(ApplicationDbContext db
		  , IHttpContextAccessor contextAccessor, IConfiguration configuration, IWebHostEnvironment hostingEnvironment, ICurrencyConversionService currencyService)
			  : base(hostingEnvironment, contextAccessor, configuration, db)
		{
			_db = db;
			_currencyService = currencyService;
			

		}
		[HttpGet]
        public IActionResult GetDriverDocuments([FromQuery] string searchText = "", int driverId = 0, int currentPageNumber = 1, int pageSize = 50)
        {
            var query = _db.DriverDocument.AsQueryable();

            // Filter by DriverId
            if (driverId > 0)
            {
                query = query.Where(dd => dd.DriverId == driverId);
            }

            // Search functionality (Example: search by LicensePlate or NationalId)
            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(dd => dd.LicensePlate.Contains(searchText) || dd.NationalId.Contains(searchText));
            }

            // Pagination
            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var driverDocuments = query.Skip((currentPageNumber - 1) * pageSize).Take(pageSize).ToList();

            var response = new PagedResponse<DriverDocument>
            {
                CurrentPageNumber = currentPageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Items = driverDocuments
            };

            return Ok(response);
        }


        [HttpGet("{id}")]
        public IActionResult GetDriverDocumentById(int id)
        {
            var driverDocument = _db.DriverDocument.FirstOrDefault(dd => dd.DriverDocuId == id);

            if (driverDocument == null)
            {
                return Ok(new { message = "DriverDocument record not found." });
            }

            return Ok(driverDocument);
        }

        [HttpPost]
        public IActionResult SaveDriverDocument([FromBody] DriverDocument driverDocument)
        {
            if (driverDocument == null)
            {
                return BadRequest("Invalid driver document data.");
            }

            try
            {
                _db.DriverDocument.Add(driverDocument);
                _db.SaveChanges();

                return Ok(new { message = "DriverDocument record saved successfully!", driverDocuId = driverDocument.DriverDocuId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("update/{id}")]
        public IActionResult UpdateDriverDocument(int id, [FromBody] DriverDocument updatedDriverDocument)
        {
            if (updatedDriverDocument == null || id != updatedDriverDocument.DriverDocuId)
            {
                return BadRequest("Invalid driver document data or ID mismatch.");
            }

            var existingDriverDocument = _db.DriverDocument.FirstOrDefault(dd => dd.DriverDocuId == id);

            if (existingDriverDocument == null)
            {
                return Ok("DriverDocument record not found.");
            }

            try
            {
                // Update properties only if they are not default values or empty
                existingDriverDocument.DriverId = updatedDriverDocument.DriverId != 0 ? updatedDriverDocument.DriverId : existingDriverDocument.DriverId;
                existingDriverDocument.LicensePlate = !string.IsNullOrEmpty(updatedDriverDocument.LicensePlate) ? updatedDriverDocument.LicensePlate : existingDriverDocument.LicensePlate;
                existingDriverDocument.LicenseExpDate = updatedDriverDocument.LicenseExpDate != default ? updatedDriverDocument.LicenseExpDate : existingDriverDocument.LicenseExpDate;
                existingDriverDocument.NationalId = !string.IsNullOrEmpty(updatedDriverDocument.NationalId) ? updatedDriverDocument.NationalId : existingDriverDocument.NationalId;
                existingDriverDocument.NationalIdExpDate = updatedDriverDocument.NationalIdExpDate != default ? updatedDriverDocument.NationalIdExpDate : existingDriverDocument.NationalIdExpDate;
                existingDriverDocument.OtherDocument = !string.IsNullOrEmpty(updatedDriverDocument.OtherDocument) ? updatedDriverDocument.OtherDocument : existingDriverDocument.OtherDocument;
                existingDriverDocument.OtherDocumentExpDate = updatedDriverDocument.OtherDocumentExpDate != default ? updatedDriverDocument.OtherDocumentExpDate : existingDriverDocument.OtherDocumentExpDate;
                existingDriverDocument.upload = !string.IsNullOrEmpty(updatedDriverDocument.upload) ? updatedDriverDocument.upload : existingDriverDocument.upload;

                _db.DriverDocument.Update(existingDriverDocument);
                _db.SaveChanges();

                return Ok(new { message = "DriverDocument record updated successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("delete/{id}")]
        public IActionResult DeleteDriverDocument(int id)
        {
            var driverDocument = _db.DriverDocument.FirstOrDefault(dd => dd.DriverDocuId == id);

            if (driverDocument == null)
            {
                return Ok(new { message = "DriverDocument record not found." });
            }

            try
            {
                _db.DriverDocument.Remove(driverDocument);
                _db.SaveChanges();

                return Ok(new { message = "DriverDocument record deleted successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


    }
}
