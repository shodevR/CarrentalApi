using Azure;
using CarRentalApi.Data;
using CarRentalApi.Model;
using CarRentalApi.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting.Internal;

namespace CarRentalApi.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class TestLocationController : ControllerBase
	{
		private readonly ApplicationDbContext _db;

		public TestLocationController(ApplicationDbContext ldb)
		{
			_db = ldb;
		}
		[HttpGet]
		public async Task<ResponseModel> AllDataList()
		{
			ResponseModel responseModel = new ResponseModel();
			try
			{
				var locations = await _db.LocationMaster
				.Select(l => new
				{
					l.Id,
					l.LocationName,      
					l.CountryName,   
					l.FullName,
					l.CurrencyCode
				})
				.ToListAsync();

				responseModel.Data = locations;
				responseModel.Status = StatusEnums.success.ToString();

			} catch (Exception ex)
			{
				responseModel.Message = ex.Message;
				responseModel.Status = StatusEnums.error.ToString();

			}

			return responseModel;
		}




	}
}
