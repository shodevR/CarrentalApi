namespace CarRentalApi.Service
{
	using CarRentalApi.Data;
	using CarRentalApi.Model;
	using Microsoft.EntityFrameworkCore;
	using Microsoft.Extensions.Configuration;
	using Microsoft.IdentityModel.Tokens;
	using Newtonsoft.Json;
	using System.IdentityModel.Tokens.Jwt;
	using System.Security.Claims;
	using System.Text;

	public class LocationSyncService : ILocationSyncService
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IConfiguration _configuration;

		public LocationSyncService(ApplicationDbContext dbContext, IHttpClientFactory httpClientFactory, IConfiguration configuration)
		{
			_dbContext = dbContext;
			_httpClientFactory = httpClientFactory;
			_configuration = configuration;
		}

		public async Task SyncLocationsAsync()
		{
			try
			{


				var config = _configuration.GetSection("LocationSync");
				var apiUrl = config["ApiUrl"];

				var token = await GetJwtTokenAsync();

				var client = _httpClientFactory.CreateClient();
				client.DefaultRequestHeaders.Authorization =
					new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await client.GetAsync(apiUrl);

				if (!response.IsSuccessStatusCode)
				{
					throw new Exception($"LocationSync API call failed: {response.StatusCode}");
				}

				var json = await response.Content.ReadAsStringAsync();
				var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(json);

				if (apiResponse?.Status?.ToLower() != "success" || apiResponse.Data == null) return;

				var apiLocations = apiResponse.Data;
				var simplifiedLocationsapi = apiLocations
					.Select(x => new
					{
						
						x.CurrencyCode
					})
					.ToList();
				var dbLocations = await _dbContext.LocationMaster.AsNoTracking().ToListAsync();
				var simplifiedLocations = dbLocations
					.Select(x => new
					{
						
						x.CurrencyCode
					})
					.ToList();

				bool areEqual = simplifiedLocationsapi
					.Select(x => x.CurrencyCode)
					.SequenceEqual(simplifiedLocations.Select(x => x.CurrencyCode));

				if (!areEqual)
				{
					foreach (var location in apiResponse.Data)
					{
						var existing = await _dbContext.LocationMaster.FirstOrDefaultAsync(b => b.Id == location.Id);

						if (existing != null)
						{
							_dbContext.Entry(existing).CurrentValues.SetValues(location);
						}
						else
						{
							await _dbContext.InsertWithIdentityAsync(location, "LocationMaster");
						}
					}

					await _dbContext.SaveChangesAsync();  // only for updates
				}
				

			}
			catch (Exception ex)
			{
				var response = new Exception(ex.Message);
			}
		}

		private bool AreLocationsEqual(BranchMaster a, BranchMaster b)
		{
			if (a == null || b == null) return false;

			return a.Id == b.Id
				&& a.LocationName == b.LocationName
				&& a.Address == b.Address
				&& a.City == b.City
				&& a.State == b.State
				&& a.Province == b.Province
				&& a.CountryCode1 == b.CountryCode1
				&& a.LocationMobileNo1 == b.LocationMobileNo1
				&& a.CountryCode2 == b.CountryCode2
				&& a.LocationMobileNo2 == b.LocationMobileNo2
				&& a.Country == b.Country
				&& a.CurrencyCode == b.CurrencyCode
				&& a.PostalCode == b.PostalCode
				&& a.CreatedBy == b.CreatedBy
				&& a.CreatedDate == b.CreatedDate
				&& a.LastModeifiedBy == b.LastModeifiedBy
				&& a.LastModeifiedDate == b.LastModeifiedDate;
		}


		public async Task<string> GetJwtTokenAsync()
		{
			var config = _configuration.GetSection("LocationSync");
			var loginUrl = config["LoginUrl"];
			var username = config["Username"];
			var password = config["Password"];
			var RouteLoc = config["RouteLoc"];

			var client = _httpClientFactory.CreateClient();

			var payload = new
			{
				UserName = username,
				Password = password,
				RouteLoc = RouteLoc
			};


			var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

			var response = await client.PostAsync(loginUrl, content);

			if (!response.IsSuccessStatusCode)
			{
				throw new Exception($"Failed to login for JWT token. Status: {response.StatusCode}");
			}

			var json = await response.Content.ReadAsStringAsync();
			var result = JsonConvert.DeserializeObject<LoginResponse>(json);

			if (result?.ResponseStatus?.ToLower() != "success" || string.IsNullOrEmpty(result?.Token))
			{
				throw new Exception("JWT token is missing or login failed.");
			}

			return result.Token;
		}

		class LoginResponse
		{
			[JsonProperty("status")]
			public string ResponseStatus { get; set; }

			[JsonProperty("data")]
			public string Token { get; set; }

			[JsonProperty("message")]
			public string Message { get; set; }
		}

		




		class ApiResponse
		{
			public string Status { get; set; }
			public List<BranchMaster> Data { get; set; }
		}
	}

}
