using Microsoft.Extensions.Caching.Memory;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace CarRentalApi.Service
{
	public class CurrencyConversionService : ICurrencyConversionService
	{
		private readonly HttpClient _httpClient;
		private readonly IMemoryCache _cache;

		public CurrencyConversionService(HttpClient httpClient, IMemoryCache cache)
		{
			_httpClient = httpClient;
			_cache = cache;
		}

		public async Task<decimal> GetConversionRateAsync(string fromCurrency, string toCurrency)
		{
			/*fromCurrency = "USD";
			toCurrency = "USD";*/
			string cacheKey = $"{fromCurrency}_{toCurrency}_rate";

			if (!_cache.TryGetValue(cacheKey, out decimal rate))
			{
				try
				{
					var requestBody = new { FromCurrency = fromCurrency, ToCurrency = toCurrency };
					var response = await _httpClient.PostAsJsonAsync("api/APIUser/GetCurrencyROE_Convert", requestBody);

					if (response.IsSuccessStatusCode)
					{
						var responseContent = await response.Content.ReadAsStringAsync();
						Console.WriteLine($"API Response: {responseContent}");

						rate = decimal.Parse(responseContent);
						_cache.Set(cacheKey, rate, TimeSpan.FromMinutes(10));
					}
					else
					{
						Console.WriteLine($"API Error: {response.StatusCode}");
						return 0m; // or throw exception if preferred
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error: {ex.Message}");
					return 0m; // or throw exception if preferred
				}
			}

			return rate;
		}


		private class CurrencyApiResponse
		{
			[JsonPropertyName("convertedValue")] // Map to lowercase
			public decimal ConvertedValue { get; set; }
		}
	}
}
