using CarRentalApi.Data;
using CarRentalApi.Model;
using CarRentalApi.Service;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace CarRentalApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehicleTypeController :BaseController
    {
        

		
		private readonly ApplicationDbContext _db;
		private readonly ICurrencyConversionService _currencyService;

		public VehicleTypeController(ApplicationDbContext db
		  , IHttpContextAccessor contextAccessor, IConfiguration configuration, IWebHostEnvironment hostingEnvironment, ICurrencyConversionService currencyService)
			  : base(hostingEnvironment, contextAccessor, configuration, db)
		{
			_db = db;
			_currencyService = currencyService;
			

		}

		[HttpPost("create")]
        public IActionResult Create([FromBody] VehicleType vehicleType)
        {
            if (vehicleType == null)
            {
                return BadRequest("Invalid vehicle type data.");
            }

            try
            {
                _db.VehicleType.Add(vehicleType);
                _db.SaveChanges();
                return Ok(new { message = "Vehicle type created successfully!", vehicleType });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("update")]
        public IActionResult Update([FromBody] VehicleType vehicleType)
        {
            if (vehicleType == null)
            {
                return BadRequest("Invalid vehicle type data.");
            }

            try
            {
                var existingVehicleType = _db.VehicleType.FirstOrDefault(vt => vt.Id == vehicleType.Id);
                if (existingVehicleType == null)
                {
                    // Return empty array with message if not found
                    return Ok(new { message = "Vehicle type not found.", vehicleTypes = new VehicleType[] { } });
                }

                existingVehicleType.TypeName = vehicleType.TypeName;
                _db.VehicleType.Update(existingVehicleType);
                _db.SaveChanges();

                return Ok(new { message = "Vehicle type updated successfully!", existingVehicleType });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("delete")]
        public IActionResult Delete([FromBody] VehicleType vehicleType)
        {
            if (vehicleType == null)
            {
                return BadRequest("Invalid vehicle type data.");
            }

            try
            {
                var existingVehicleType = _db.VehicleType.FirstOrDefault(vt => vt.Id == vehicleType.Id);
                if (existingVehicleType == null)
                {
                    // Return empty array with message if not found
                    return Ok(new { message = "Vehicle type not found.", vehicleTypes = new VehicleType[] { } });
                }

                _db.VehicleType.Remove(existingVehicleType);
                _db.SaveChanges();

                return Ok(new { message = "Vehicle type deleted successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("get")]
        public IActionResult Get()
        {
            try
            {
                var vehicleTypes = _db.VehicleType.OrderByDescending(vt => vt.Id).ToList();
                return Ok(vehicleTypes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
