using CarRentalApi.Data;
using CarRentalApi.Model;
using CarRentalApi.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting.Internal;

namespace CarRentalApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    
    public class VehicleFeaturesController :BaseController
    {
		
		private readonly ApplicationDbContext _db;
		private readonly ICurrencyConversionService _currencyService;

		public VehicleFeaturesController(ApplicationDbContext db
		  , IHttpContextAccessor contextAccessor, IConfiguration configuration, IWebHostEnvironment hostingEnvironment, ICurrencyConversionService currencyService)
			  : base(hostingEnvironment, contextAccessor, configuration, db)
		{
			_db = db;
			_currencyService = currencyService;
			

		}


		// GET: api/VehicleFeatures
		[HttpGet]
        public async Task<ActionResult<IEnumerable<VehicleFeatures>>> GetVehicleFeatures()
        {

            return await _db.VehicleFeatures.OrderByDescending(v => v.VehicleFeatureId).ToListAsync();
        }

        // GET: api/VehicleFeatures/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<VehicleFeatures>> GetVehicleFeature(int id)
        {
            var vehicleFeature = await _db.VehicleFeatures.FindAsync(id);

            if (vehicleFeature == null)
            {
                return Ok("No Feature with given Id");
            }

            return vehicleFeature;
        }

        // POST: api/VehicleFeatures
        [HttpPost]
        public async Task<ActionResult<VehicleFeatures>> PostVehicleFeature(VehicleFeatures vehicleFeature)
        {
            // Check if a feature with the same name already exists
            if (await _db.VehicleFeatures.AnyAsync(vf => vf.VehicleFeatureName == vehicleFeature.VehicleFeatureName))
            {
                return Conflict(new { Message = "A feature with the same name already exists." });
            }

            _db.VehicleFeatures.Add(vehicleFeature);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVehicleFeature), new { id = vehicleFeature.VehicleFeatureId }, vehicleFeature);
        }

        // PUT: api/VehicleFeatures/{id}
        [HttpPost("update/{id}")]
        public async Task<IActionResult> PutVehicleFeature(int id, VehicleFeatures vehicleFeature)
        {
            if (id != vehicleFeature.VehicleFeatureId)
            {
                return BadRequest();
            }

            _db.Entry(vehicleFeature).State = EntityState.Modified;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VehicleFeatureExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/VehicleFeatures/{id}
        [HttpPost("delete/{id}")]
        public async Task<IActionResult> DeleteVehicleFeature(int id)
        {
            var vehicleFeature = await _db.VehicleFeatures.FindAsync(id);
            if (vehicleFeature == null)
            {
                return Ok();
            }

            _db.VehicleFeatures.Remove(vehicleFeature);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        private bool VehicleFeatureExists(int id)
        {
            return _db.VehicleFeatures.Any(e => e.VehicleFeatureId == id);
        }
    }
}
