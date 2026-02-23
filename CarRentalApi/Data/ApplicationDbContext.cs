using CarRentalApi.Model;
using CarRentalApi.Model.CarRentalApi.Model;
using Microsoft.EntityFrameworkCore;

namespace CarRentalApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Driver>Driver { get; set; }
        public DbSet<Vehicle> Vehicle { get; set; }
        public DbSet<VehicleDocument> VehicleDocument { get; set; }
        public DbSet<VehicleMaintenance> VehicleMaintenance { get; set; }
        public DbSet<Booking> Booking { get; set; }
        public DbSet<PriceMaster> PriceMaster { get; set; }
        public DbSet<Client> Client { get; set; }
        public DbSet<DriverDocument> DriverDocument { get; set; }
        public DbSet<CheckList> CheckList { get; set; } 
        public DbSet<DriverLeave> DriverLeave { get; set; }
        public DbSet<Document> Document { get; set; }
        public DbSet<DriverDocuments> DriverDocuments { get; set; }
        public DbSet<VehicleType> VehicleType { get; set; }
        public DbSet<VehicleFeatures> VehicleFeatures { get; set; }
        public DbSet<BookingModify> BookingModify { get; set; }
		public DbSet<BranchMaster> LocationMaster { get; set; }
		public DbSet<CountryMaster> TP_CountryMaster { get; set; }






	}
	public static class DbContextExtensions
	{
		public static async Task InsertWithIdentityAsync<TEntity>(
			this DbContext db,
			TEntity entity,
			string tableName)
			where TEntity : class
		{
			using var transaction = await db.Database.BeginTransactionAsync();

			try
			{
				await db.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT {tableName} ON");

				db.Set<TEntity>().Add(entity);
				await db.SaveChangesAsync();

				await db.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT {tableName} OFF");

				await transaction.CommitAsync();
			}
			catch
			{
				await transaction.RollbackAsync();
				throw;
			}
		}
	}

}
