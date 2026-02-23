namespace CarRentalApi.Service
{
	public interface ICurrencyConversionService
	{
		public Task<decimal> GetConversionRateAsync(string fromCurrency, string toCurrency);
	}
}
