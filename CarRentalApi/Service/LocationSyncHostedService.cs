namespace CarRentalApi.Service
{
	public class LocationSyncHostedService : BackgroundService
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogger<LocationSyncHostedService> _logger;

		public LocationSyncHostedService(IServiceProvider serviceProvider, ILogger<LocationSyncHostedService> logger)
		{
			_serviceProvider = serviceProvider;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					using var scope = _serviceProvider.CreateScope();
					var syncService = scope.ServiceProvider.GetRequiredService<ILocationSyncService>();
					await syncService.SyncLocationsAsync();

					_logger.LogInformation("Location sync completed at: {time}", DateTimeOffset.Now);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error during location sync");
				}

				await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
			}
		}
	}

}
