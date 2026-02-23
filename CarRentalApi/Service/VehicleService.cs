namespace CarRentalApi.Service
{
    using CarRentalApi.Data;
    using CarRentalApi.Model;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class VehicleService
    {
        private readonly ApplicationDbContext _db;

        public VehicleService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<string>> GetExpiringDocumentsAsync(int vehicleId, DateTime dateFrom, DateTime dateTo)
        {
            var messages = new List<string>();

            // Fetch documents for the given vehicle
            var documents = await _db.Document
                .Where(d => d.VehicleId == vehicleId && d.ExpireDate.HasValue)
                .ToListAsync();

            // Get current date and calculate the 7-day range
            var today = DateTime.Now;
            var next7Days = dateTo.AddDays(7);

            foreach (var document in documents)
            {
                var expireDate = document.ExpireDate.Value;

                // Check if document expiry falls within the specified range or next 7 days
                if ((expireDate >= dateFrom && expireDate <= dateTo) ||
                    (expireDate >= dateTo && expireDate <= next7Days))
                {
                    var message = $"The document \"{document.Name}\" for vehicle ID {vehicleId} will expire on {expireDate:dd-MM-yyyy}.";
                    messages.Add(message);
                }
            }

            return messages;
        }
    }

}
