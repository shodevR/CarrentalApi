using CarRentalApi.Data;
using CarRentalApi.Service;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CarRentalApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageUploadController : BaseController
    {
		
		private readonly ApplicationDbContext _db;
		private readonly IImageUploadService _imageUploadService;

		public ImageUploadController(ApplicationDbContext db, IImageUploadService imageUploadService, IHttpContextAccessor contextAccessor, IConfiguration configuration, IWebHostEnvironment hostingEnvironment) : base(hostingEnvironment, contextAccessor, configuration, db)
		{
			_db = db;
			_imageUploadService = imageUploadService;
			
		}

		[HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is empty or not provided.");
            }

            try
            {
                // Use the service to upload the file
                var uploadedFilePath = await _imageUploadService.UploadImageAsync(file);

                // Return the uploaded file URL
                return Ok( uploadedFilePath );
            }
            catch (Exception ex)
            {
                // Log the exception if necessary
                // _logger.LogError(ex, "Error uploading image.");

                return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "An error occurred while uploading the file.", Details = ex.Message });
            }
        }
    }
}
