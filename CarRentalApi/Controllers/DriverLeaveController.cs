using CarRentalApi.Data;
using CarRentalApi.Model;
using CarRentalApi.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CarRentalApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DriverLeaveController :BaseController
    {
		
		private readonly ApplicationDbContext _db;
		private readonly ICurrencyConversionService _currencyService;

		public DriverLeaveController(ApplicationDbContext db
		  , IHttpContextAccessor contextAccessor, IConfiguration configuration, IWebHostEnvironment hostingEnvironment, ICurrencyConversionService currencyService)
			  : base(hostingEnvironment, contextAccessor, configuration, db)
		{
			_db = db;
			_currencyService = currencyService;
			

		}

		[HttpGet]
        public IActionResult GetDriverLeaves([FromQuery] string searchText = "", int driverId = 0, [FromQuery] string branchIds = "", int currentPageNumber = 1, int pageSize = 50)
        {
            var query = _db.DriverLeave.AsQueryable();

            // Filter by DriverId
            if (driverId > 0)
            {
                query = query.Where(dl => dl.DriverId == driverId);
            }

            if (!string.IsNullOrEmpty(branchIds))
            {
                // Parse comma-separated string into a list of integers
                var branchIdList = branchIds.Split(',')
                                            .Select(id => int.Parse(id))
                                            .ToList();

                // Filter by the list of branch IDs
                query = query.Where(v => branchIdList.Contains(v.BranchId));
            }

            // Search functionality (Example: search by DriverName or Reason)
            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(dl => dl.DriverName.ToString().Contains(searchText) || dl.Reason.Contains(searchText));
            }

            // Always order by DriverLeaveId in descending order
            query = query.OrderByDescending(dl => dl.DriverLeaveId);

            // Pagination
            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var driverLeaves = query.Skip((currentPageNumber - 1) * pageSize).Take(pageSize).ToList();

            var response = new PagedResponse<DriverLeave>
            {
                CurrentPageNumber = currentPageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Items = driverLeaves
            };

            return Ok(response);
        }

        [HttpGet("{id}")]
        public IActionResult GetDriverLeaveById(int id)
        {
            var driverLeave = _db.DriverLeave.FirstOrDefault(dl => dl.DriverLeaveId == id);

            if (driverLeave == null)
            {
                return Ok(new { message = "DriverLeave record not found." });
            }

            return Ok(driverLeave);
        }

        /* [HttpPost]
         public IActionResult SaveDriverLeave([FromBody] DriverLeave driverLeave)
         {
             if (driverLeave == null)
             {
                 return BadRequest("Invalid driver leave data.");
             }

             try
             {
                 _db.DriverLeave.Add(driverLeave);
                 _db.SaveChanges();

                 return Ok(new { message = "DriverLeave record saved successfully!", driverLeaveId = driverLeave.DriverLeaveId });
             }
             catch (Exception ex)
             {
                 return StatusCode(500, $"Internal server error: {ex.Message}");
             }
         }*/

        [HttpPost]
        public IActionResult SaveDriverLeave([FromBody] DriverLeave driverLeave)
        {
            if (driverLeave == null || driverLeave.DriverId <= 0)
            {
                return BadRequest("Invalid driver leave data or missing DriverId.");
            }

            try
            {
                // Retrieve the Driver record to get the BranchId
                var driver = _db.Driver.FirstOrDefault(d => d.DriverId == driverLeave.DriverId);
                if (driver == null)
                {
                    return BadRequest(new { message = "Driver not found." });
                }

                // Check for overlapping leave records
                bool hasOverlap = _db.DriverLeave.Any(dl =>
                    dl.DriverId == driverLeave.DriverId &&
                    ((driverLeave.LeaveDateFrom >= dl.LeaveDateFrom && driverLeave.LeaveDateFrom <= dl.LeaveDateTo) ||
                     (driverLeave.LeaveDateTo >= dl.LeaveDateFrom && driverLeave.LeaveDateTo <= dl.LeaveDateTo) ||
                     (driverLeave.LeaveDateFrom <= dl.LeaveDateFrom && driverLeave.LeaveDateTo >= dl.LeaveDateTo)));

                if (hasOverlap)
                {
                    return BadRequest("The driver already has a leave scheduled in the given date range.");
                }

                // Automatically set the BranchId from the Driver record
                driverLeave.BranchId = driver.BranchId;
                driverLeave.CreatedBy = (int)this.UserID;
                driverLeave.CreatedByName = this.UserEmail;

                _db.DriverLeave.Add(driverLeave);
                _db.SaveChanges();

                return Ok(new { message = "DriverLeave record saved successfully!", driverLeaveId = driverLeave.DriverLeaveId });
            }
            catch (Exception ex)
            {
                return StatusCode(500,$"Internal server error: {ex.Message}");
            }
        }

        /*  [HttpPost("update/{id}")]
          public IActionResult UpdateDriverLeave(int id, [FromBody] DriverLeave updatedDriverLeave)
          {
              if (updatedDriverLeave == null || id != updatedDriverLeave.DriverLeaveId)
              {
                  return BadRequest("Invalid driver leave data or ID mismatch.");
              }

              var existingDriverLeave = _db.DriverLeave.FirstOrDefault(dl => dl.DriverLeaveId == id);

              if (existingDriverLeave == null)
              {
                  return Ok("DriverLeave record not found.");
              }

              try
              {
                  // Update properties only if they are provided
                  existingDriverLeave.DriverId = updatedDriverLeave.DriverId != 0 ? updatedDriverLeave.DriverId : existingDriverLeave.DriverId;
                  existingDriverLeave.DriverName = updatedDriverLeave.DriverName != default ? updatedDriverLeave.DriverName : existingDriverLeave.DriverName;
                  existingDriverLeave.LeaveDateFrom = updatedDriverLeave.LeaveDateFrom != default ? updatedDriverLeave.LeaveDateFrom : existingDriverLeave.LeaveDateFrom;
                  existingDriverLeave.LeaveDateTo = updatedDriverLeave.LeaveDateTo != default ? updatedDriverLeave.LeaveDateTo : existingDriverLeave.LeaveDateTo;
                  existingDriverLeave.Reason = !string.IsNullOrEmpty(updatedDriverLeave.Reason) ? updatedDriverLeave.Reason : existingDriverLeave.Reason;
                  existingDriverLeave.ApprovedBy = !string.IsNullOrEmpty(updatedDriverLeave.ApprovedBy) ? updatedDriverLeave.ApprovedBy : existingDriverLeave.ApprovedBy;
                  existingDriverLeave.StatusFlag = updatedDriverLeave.StatusFlag != false ? updatedDriverLeave.StatusFlag : existingDriverLeave.StatusFlag;

                  _db.DriverLeave.Update(existingDriverLeave);
                  _db.SaveChanges();

                  return Ok(new { message = "DriverLeave record updated successfully!" });
              }
              catch (Exception ex)
              {
                  return StatusCode(500, $"Internal server error: {ex.Message}");
              }
          }*/
        [HttpPost("update/{id}")]
        public IActionResult UpdateDriverLeave(int id, [FromBody] DriverLeave updatedDriverLeave)
        {
            if (updatedDriverLeave == null || id != updatedDriverLeave.DriverLeaveId)
            {
                return BadRequest("Invalid driver leave data or ID mismatch.");
            }

            var existingDriverLeave = _db.DriverLeave.FirstOrDefault(dl => dl.DriverLeaveId == id);

            if (existingDriverLeave == null)
            {
                return Ok("DriverLeave record not found.");
            }

            try
            {
                // If DriverId is updated, fetch the BranchId from the Driver model
                if (updatedDriverLeave.DriverId != 0 && updatedDriverLeave.DriverId != existingDriverLeave.DriverId)
                {
                    var driver = _db.Driver.FirstOrDefault(d => d.DriverId == updatedDriverLeave.DriverId);
                    if (driver == null)
                    {
                        return Ok(new { message = "Driver not found." });
                    }

                    existingDriverLeave.BranchId = driver.BranchId; // Update the BranchId
                }

                // Update other properties only if they are provided
                existingDriverLeave.DriverId = updatedDriverLeave.DriverId != 0 ? updatedDriverLeave.DriverId : existingDriverLeave.DriverId;
                existingDriverLeave.DriverName = updatedDriverLeave.DriverName != default ? updatedDriverLeave.DriverName : existingDriverLeave.DriverName;
                existingDriverLeave.LeaveDateFrom = updatedDriverLeave.LeaveDateFrom != default ? updatedDriverLeave.LeaveDateFrom : existingDriverLeave.LeaveDateFrom;
                existingDriverLeave.LeaveDateTo = updatedDriverLeave.LeaveDateTo != default ? updatedDriverLeave.LeaveDateTo : existingDriverLeave.LeaveDateTo;
                existingDriverLeave.Reason = !string.IsNullOrEmpty(updatedDriverLeave.Reason) ? updatedDriverLeave.Reason : existingDriverLeave.Reason;
                existingDriverLeave.ApprovedBy = !string.IsNullOrEmpty(updatedDriverLeave.ApprovedBy) ? updatedDriverLeave.ApprovedBy : existingDriverLeave.ApprovedBy;
                existingDriverLeave.CreatedBy = (int)this.UserID;
                existingDriverLeave.CreatedByName = this.UserEmail;
                existingDriverLeave.StatusFlag = updatedDriverLeave.StatusFlag;

                _db.DriverLeave.Update(existingDriverLeave);
                _db.SaveChanges();

                return Ok(new { message = "DriverLeave record updated successfully!" });
            }
            catch (Exception ex)
            {
                return Ok( $"Internal server error: {ex.Message}");
            }
        }


        [HttpPost("delete/{id}")]
        public IActionResult DeleteDriverLeave(int id)
        {
            var driverLeave = _db.DriverLeave.FirstOrDefault(dl => dl.DriverLeaveId == id);

            if (driverLeave == null)
            {
                return Ok(new { message = "DriverLeave record not found." });
            }

            try
            {
                _db.DriverLeave.Remove(driverLeave);
                _db.SaveChanges();

                return Ok(new { message = "DriverLeave record deleted successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        // GET: api/driverleave/bydriver/{driverId}
        [HttpGet("bydriver/{driverId}")]
        public IActionResult GetDriverLeavesByDriverId(int driverId)
        {
            // Retrieve leave records for the specified driver
            var driverLeaves = _db.DriverLeave.Where(dl => dl.DriverId == driverId).OrderByDescending(dl => dl.DriverLeaveId).ToList();

            if (driverLeaves == null || driverLeaves.Count == 0)
            {
                return Ok(new { message = "No leave records found for the specified driver." });
            }

            return Ok(driverLeaves);
        }



    }
}
