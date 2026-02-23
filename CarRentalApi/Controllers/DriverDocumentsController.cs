using Microsoft.AspNetCore.Mvc;
using CarRentalApi.Data;
using CarRentalApi.Model;
using CarRentalApi.Service;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CarRentalApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DriverDocumentsController :BaseController
    {
		
		private readonly ApplicationDbContext _db;
		private readonly IImageUploadService _imageUploadService;

		public DriverDocumentsController(ApplicationDbContext db, IImageUploadService imageUploadService, IHttpContextAccessor contextAccessor, IConfiguration configuration, IWebHostEnvironment hostingEnvironment) : base(hostingEnvironment, contextAccessor, configuration, db)
		{
			_db = db;
			_imageUploadService = imageUploadService;
			
		}

		[HttpPost]
        public async Task<IActionResult> UploadDriverDocument([FromForm] DriverDocuments driverDocument, IFormFile file)
        {
            if (file == null || driverDocument == null)
            {
                return BadRequest("Invalid driver document data or file.");
            }

            try
            {
                // Save the file and get its path
                string filePath = await _imageUploadService.UploadImageAsync(file);
                driverDocument.DocumentPath = filePath;

                // Set UploadDate if it wasn't set by the client
                if (driverDocument.UploadDate == default)
                {
                    driverDocument.UploadDate = DateTime.UtcNow;
                }

                // Add driver document to the database
                _db.DriverDocuments.Add(driverDocument);
                await _db.SaveChangesAsync();

                return Ok(new { message = "Driver document uploaded successfully!", driverDocumentId = driverDocument.DriverDocumentsId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("update/{id}")]
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
                    /*// Delete old file if it exists
                    if (!string.IsNullOrEmpty(driverDocument.DocumentPath))
                    {
                        DeleteFile(driverDocument.DocumentPath);
                    }*/

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
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("driver/{driverId}")]
        public IActionResult GetDocumentsByDriverId(int driverId)
        {
            var driverDocuments = _db.DriverDocuments.Where(dd => dd.DriverId == driverId).ToList();
            if (!driverDocuments.Any())
            {
                return Ok(new { message = "No documents found for this driver." });
            }

            return Ok(driverDocuments);
        }

        [HttpPost("delete/{id}")]
        public async Task<IActionResult> DeleteDriverDocument(int id)
        {
            var driverDocument = await _db.DriverDocuments.FindAsync(id);
            if (driverDocument == null)
            {
                return Ok("Driver document not found.");
            }

            try
            {
               /* // Delete file from storage
                DeleteFile(driverDocument.DocumentPath);*/

                // Remove driver document from the database
                _db.DriverDocuments.Remove(driverDocument);
                await _db.SaveChangesAsync();

                return Ok(new { message = "Driver document deleted successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /*private void DeleteFile(string filePath)
        {
            var fullPath = Path.Combine(_imageUploadService.RootPath, filePath);
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }*/
        // GET: api/DriverDocuments/expired-or-expiring-soon/{driverId?}
        
        [HttpGet]
        [Route("expired-or-expiring-soon")]
        public IActionResult GetExpiredOrExpiringSoonDriverDocuments(int? driverId)
        {
            try
            {
                // Get today's date and the date 20 days from now
                var today = DateTime.UtcNow.Date;
                var twentyDaysLater = today.AddDays(20);

                // Query to get driver documents and join with drivers
                var query = from doc in _db.DriverDocuments
                            join drv in _db.Driver on doc.DriverId equals drv.DriverId
                            where doc.ExpireDate != null &&
                                  (doc.ExpireDate <= today || (doc.ExpireDate <= twentyDaysLater && doc.ExpireDate >= today))
                            select new
                            {
                                doc.DriverDocumentsId,
                                doc.DriverId,
                                doc.ExpireDate,
                                doc.UploadDate,
                                doc.DocumentPath,
                                doc.Name,
                                DriverName = drv.DriverName
                            };

                if (driverId.HasValue)
                {
                    // Filter by driverId if provided
                    query = query.Where(d => d.DriverId == driverId.Value);
                }

                var expiringDriverDocuments = query.ToList();

                if (!expiringDriverDocuments.Any())
                {
                    return Ok(new
                    {
                        message = driverId.HasValue ?
                                     $"No documents for driver {driverId} are expired or expiring within the next 20 days." :
                                     "No documents are expired or expiring within the next 20 days."
                    });
                }

                return Ok(expiringDriverDocuments);
            }
            catch (Exception ex)
            {
                return Ok(new { message = $"Internal server error: {ex.Message}" });
            }
        }



    }

}
