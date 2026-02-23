using CarRentalApi.Data;
using CarRentalApi.Model;
using CarRentalApi.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CarRentalApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehicleDocumentController :BaseController
    {
		
		private readonly ApplicationDbContext _db;
		private readonly ICurrencyConversionService _currencyService;

		public VehicleDocumentController(ApplicationDbContext db
		  , IHttpContextAccessor contextAccessor, IConfiguration configuration, IWebHostEnvironment hostingEnvironment, ICurrencyConversionService currencyService)
			  : base(hostingEnvironment, contextAccessor, configuration, db)
		{
			_db = db;
			_currencyService = currencyService;
			

		}

		[HttpGet]
        public IActionResult GetVehicleDocuments([FromQuery] string searchText = "", int vehicleId = 0, int currentPageNumber = 1, int pageSize = 50)
        {
            var query = _db.VehicleDocument.AsQueryable();

            // Filter by VehicleId
            if (vehicleId > 0)
            {
                query = query.Where(vd => vd.VehicleId == vehicleId);
            }

            // Search functionality (Example: filter by LicensePlate or Insurance)
            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(vd => vd.LicensePlate.Contains(searchText)
                                       || vd.Insurance.Contains(searchText));
            }

            // Pagination
            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var vehicleDocuments = query.Skip((currentPageNumber - 1) * pageSize).Take(pageSize).ToList();

            var response = new PagedResponse<VehicleDocument>
            {
                CurrentPageNumber = currentPageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Items = vehicleDocuments
            };

            return Ok(response);
        }

        [HttpGet("{id}")]
        public IActionResult GetVehicleDocumentById(int id)
        {
            var vehicleDocument = _db.VehicleDocument.FirstOrDefault(vd => vd.DocumentId == id);

            if (vehicleDocument == null)
            {
                return Ok(new { message = "VehicleDocument not found." });
            }

            return Ok(vehicleDocument);
        }

        [HttpPost]
        public IActionResult SaveVehicleDocument([FromBody] VehicleDocument vehicleDocument)
        {
            if (vehicleDocument == null)
            {
                return BadRequest("Invalid vehicle document data.");
            }

            try
            {
                _db.VehicleDocument.Add(vehicleDocument);
                _db.SaveChanges();

                return Ok(new { message = "VehicleDocument saved successfully!", documentId = vehicleDocument.DocumentId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("update/{id}")]
        public IActionResult UpdateVehicleDocument(int id, [FromBody] VehicleDocument updatedDocument)
        {
            if (updatedDocument == null || id != updatedDocument.DocumentId)
            {
                return BadRequest("Invalid vehicle document data or ID mismatch.");
            }

            var existingDocument = _db.VehicleDocument.FirstOrDefault(vd => vd.DocumentId == id);

            if (existingDocument == null)
            {
                return Ok("VehicleDocument not found.");
            }

            try
            {
                // Update properties only if they are provided
                existingDocument.VehicleId = updatedDocument.VehicleId != 0 ? updatedDocument.VehicleId : existingDocument.VehicleId;
                existingDocument.LicensePlate = !string.IsNullOrEmpty(updatedDocument.LicensePlate) ? updatedDocument.LicensePlate : existingDocument.LicensePlate;
                existingDocument.LicensePlateExp = !string.IsNullOrEmpty(updatedDocument.LicensePlateExp) ? updatedDocument.LicensePlateExp : existingDocument.LicensePlateExp;
                existingDocument.RegistratingPapers = !string.IsNullOrEmpty(updatedDocument.RegistratingPapers) ? updatedDocument.RegistratingPapers : existingDocument.RegistratingPapers;
                existingDocument.RegistratingPapersExp = !string.IsNullOrEmpty(updatedDocument.RegistratingPapersExp) ? updatedDocument.RegistratingPapersExp : existingDocument.RegistratingPapersExp;
                existingDocument.Insurance = !string.IsNullOrEmpty(updatedDocument.Insurance) ? updatedDocument.Insurance : existingDocument.Insurance;
                existingDocument.InsuranceExp = !string.IsNullOrEmpty(updatedDocument.InsuranceExp) ? updatedDocument.InsuranceExp : existingDocument.InsuranceExp;
                existingDocument.MaintenanceReceipts = !string.IsNullOrEmpty(updatedDocument.MaintenanceReceipts) ? updatedDocument.MaintenanceReceipts : existingDocument.MaintenanceReceipts;
                existingDocument.MaintenanceReceiptsExp = !string.IsNullOrEmpty(updatedDocument.MaintenanceReceiptsExp) ? updatedDocument.MaintenanceReceiptsExp : existingDocument.MaintenanceReceiptsExp;
                existingDocument.OtherDocs = !string.IsNullOrEmpty(updatedDocument.OtherDocs) ? updatedDocument.OtherDocs : existingDocument.OtherDocs;
                existingDocument.OtherDocsExp = !string.IsNullOrEmpty(updatedDocument.OtherDocsExp) ? updatedDocument.OtherDocsExp : existingDocument.OtherDocsExp;
                existingDocument.StatusFlag = updatedDocument.StatusFlag;

                _db.VehicleDocument.Update(existingDocument);
                _db.SaveChanges();

                return Ok(new { message = "VehicleDocument updated successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("delete/{id}")]
        public IActionResult DeleteVehicleDocument(int id)
        {
            var vehicleDocument = _db.VehicleDocument.FirstOrDefault(vd => vd.DocumentId == id);

            if (vehicleDocument == null)
            {
                return Ok(new { message = "VehicleDocument not found." });
            }

            try
            {
                _db.VehicleDocument.Remove(vehicleDocument);
                _db.SaveChanges();

                return Ok(new { message = "VehicleDocument deleted successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}