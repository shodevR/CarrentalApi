using CarRentalApi.Data;
using CarRentalApi.Model;
using CarRentalApi.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CarRentalApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientController :BaseController
    {
		
		private readonly ApplicationDbContext _db;
		private readonly ICurrencyConversionService _currencyService;

		public ClientController(ApplicationDbContext db
		  , IHttpContextAccessor contextAccessor, IConfiguration configuration, IWebHostEnvironment hostingEnvironment, ICurrencyConversionService currencyService)
			  : base(hostingEnvironment, contextAccessor, configuration, db)
		{
			_db = db;
			_currencyService = currencyService;
			

		}

		// Get all clients with search, pagination, and optional BranchId filter
		[HttpGet]
        public IActionResult GetClients([FromQuery] string searchText = "", int currentPageNumber = 1, int pageSize = 50, [FromQuery] string branchIds = "")
        {
            var query = _db.Client.AsQueryable();

            // Optional BranchId filter
            if (!string.IsNullOrEmpty(branchIds))
            {
                // Parse comma-separated string into a list of integers
                var branchIdList = branchIds.Split(',')
                                            .Select(id => int.Parse(id))
                                            .ToList();

                // Filter by the list of branch IDs
                query = query.Where(v => branchIdList.Contains((int)v.BranchId));
            }

            // Search functionality
            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(c => c.FirstName.Contains(searchText)
                                       || c.LastName.Contains(searchText)
                                       || c.Email.Contains(searchText)
                                       || c.Mobile.Contains(searchText));
            }

            query = query.OrderByDescending(c => c.ClientId);

            // Pagination
            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var clients = query.Skip((currentPageNumber - 1) * pageSize).Take(pageSize).ToList();

            var response = new
            {
                CurrentPageNumber = currentPageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Items = clients
            };

            return Ok(response);
        }


        // Get client by ID
        [HttpGet("{id}")]
        public IActionResult GetClientById(int id)
        {
            var client = _db.Client.FirstOrDefault(c => c.ClientId == id);

            if (client == null)
            {
                return NotFound(new { message = "Client not found." });
            }

            return Ok(client);
        }

        // Save a new client
        [HttpPost]
        public IActionResult SaveClient([FromBody] Client client)
        {
            if (client == null)
            {
                return BadRequest("Invalid client data.");
            }

            try
            {
                _db.Client.Add(client);
                _db.SaveChanges();

                return Ok(new { message = "Client saved successfully!", clientId = client.ClientId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Update an existing client
        [HttpPost("update/{id}")]
        public IActionResult UpdateClient(int id, [FromBody] Client client)
        {
            if (client == null || client.ClientId != id)
            {
                return BadRequest("Invalid client data or ID mismatch.");
            }

            var existingClient = _db.Client.FirstOrDefault(c => c.ClientId == id);

            if (existingClient == null)
            {
                return NotFound(new { message = "Client not found." });
            }

            try
            {
                // Update client properties
                existingClient.FirstName = client.FirstName;
                existingClient.LastName = client.LastName;
                existingClient.Email = client.Email;
                existingClient.Mobile = client.Mobile;
                existingClient.Date = client.Date;
                existingClient.ReferedBy = client.ReferedBy;
                existingClient.BusinessProposal = client.BusinessProposal;
                existingClient.CompanyName = client.CompanyName;
                existingClient.CompanyAddress = client.CompanyAddress;
                existingClient.ComapanyType = client.ComapanyType;
                existingClient.Designation = client.Designation;
                existingClient.CreatedBy = (int)this.UserID;
                existingClient.CreatedByName = this.UserEmail;
                existingClient.StatusFlag = client.StatusFlag;

                _db.Client.Update(existingClient);
                _db.SaveChanges();

                return Ok(new { message = "Client updated successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Delete a client
        [HttpPost("delete/{id}")]
        public IActionResult DeleteClient(int id)
        {
            var client = _db.Client.FirstOrDefault(c => c.ClientId == id);

            if (client == null)
            {
                return NotFound(new { message = "Client not found." });
            }

            try
            {
                _db.Client.Remove(client);
                _db.SaveChanges();

                return Ok(new { message = "Client deleted successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
