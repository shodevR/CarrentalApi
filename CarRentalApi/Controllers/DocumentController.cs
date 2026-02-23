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
    public class DocumentController :BaseController
    {
        
        private readonly ApplicationDbContext _db;
        private readonly IImageUploadService _imageUploadService;

        public DocumentController(ApplicationDbContext db, IImageUploadService imageUploadService, IHttpContextAccessor contextAccessor, IConfiguration configuration, IWebHostEnvironment hostingEnvironment) : base(hostingEnvironment, contextAccessor, configuration, db)
        {
            _db = db;
            _imageUploadService = imageUploadService;
			
		}

        [HttpPost]
        public async Task<IActionResult> UploadDocument([FromForm] Document document, IFormFile file)
        {
            if (file == null || document == null)
            {
                return BadRequest("Invalid document data or file.");
            }

            try
            {
                // Save the file and get its path
                string filePath = await _imageUploadService.UploadImageAsync(file);
                document.DocumentPath = filePath;

                // Set UploadDate if it wasn't set by the client
                if (document.UploadDate == default)
                {
                    document.UploadDate = DateTime.UtcNow;
                }

                // Add document to the database
                _db.Document.Add(document);
                await _db.SaveChangesAsync();

                return Ok(new { message = "Document uploaded successfully!", documentId = document.DocumentId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpPost("update/{id}")]
        public async Task<IActionResult> UpdateDocument(int id, [FromForm] Document updatedDocument, IFormFile file)
        {
            var document = await _db.Document.FindAsync(id);
            if (document == null)
            {
                return Ok(new { message = "Document not found." });
            }

            try
            {
                // Update document properties
                document.Name = updatedDocument.Name ?? document.Name;
                document.VehicleId = updatedDocument.VehicleId ?? document.VehicleId;
                document.ExpireDate = updatedDocument.ExpireDate ?? document.ExpireDate;

                // If a new file is provided, upload and update the DocumentPath
                if (file != null)
                {
                    // Delete old file if it exists
                   /* if (!string.IsNullOrEmpty(document.DocumentPath))
                    {
                        // Use your existing DeleteFile method
                        DeleteFile(document.DocumentPath);
                    }
*/
                    // Save new file and update DocumentPath
                    string newFilePath = await _imageUploadService.UploadImageAsync(file);
                    document.DocumentPath = newFilePath;
                }

                // Update document in the database
                _db.Document.Update(document);
                await _db.SaveChangesAsync();

                return Ok(new { message = "Document updated successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        // GET: api/Document/vehicle/{vehicleId}
        [HttpGet("vehicle/{vehicleId}")]
        public IActionResult GetDocumentsByVehicleId(int vehicleId)
        {
            var documents = _db.Document.Where(d => d.VehicleId == vehicleId).ToList();
            if (!documents.Any())
            {
                return Ok(new { message = "No documents found for this vehicle." });
            }

            return Ok(documents);
        }

        // DELETE: api/Document/{id}
        [HttpPost("delete/{id}")]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var document = await _db.Document.FindAsync(id);
            if (document == null)
            {
                return Ok("Document not found.");
            }

            /*// Delete file from storage
            DeleteFile(document.DocumentPath);*/

            // Remove document from the database
            _db.Document.Remove(document);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Document deleted successfully!" });
        }

        private async Task<string> SaveFileAsync(IFormFile file)
        {
            return await _imageUploadService.UploadImageAsync(file);
        }

       /* private void DeleteFile(string filePath)
        {
            var fullPath = Path.Combine(_imageUploadService.RootPath, filePath);
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }*/
        [HttpGet]
        [Route("expired-or-expiring-soon")]
        public IActionResult GetExpiredOrExpiringSoonDocumentsByVehicle(int? vehicleId)
        {
            try
            {
                // Get today's date and the date 20 days from now
                var today = DateTime.UtcNow.Date;
                var twentyDaysLater = today.AddDays(20);

                // Query to get documents and join with vehicles
                var query = from doc in _db.Document
                            join veh in _db.Vehicle on doc.VehicleId equals veh.VehicleId
                            where doc.ExpireDate != null &&
                                  (doc.ExpireDate <= today || (doc.ExpireDate <= twentyDaysLater && doc.ExpireDate >= today))
                            select new
                            {
                                doc.DocumentId,
                                doc.VehicleId,
                                doc.ExpireDate,
                                doc.DocumentPath,
                                doc.UploadDate,
                                doc.Name,
                                VehicleName = veh.VehicleName,
                                VehicleNumber = veh.VehicleNumber
                            };

                if (vehicleId.HasValue)
                {
                    // Filter by vehicleId if provided
                    query = query.Where(d => d.VehicleId == vehicleId.Value);
                }

                var expiringDocuments = query.ToList();

                if (!expiringDocuments.Any())
                {
                    return Ok(new
                    {
                        message = vehicleId.HasValue ?
                                     $"No documents for vehicle {vehicleId} are expired or expiring within the next 20 days." :
                                     "No documents are expired or expiring within the next 20 days."
                    });
                }

                return Ok(expiringDocuments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }


    }
}
