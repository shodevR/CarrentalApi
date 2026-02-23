using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace CarRentalApi.Model
{
	public class BranchMaster
	{
		
		public int Id { get; set; }
		
		[Required]
		public string? LocationName { get; set; }

		public string? Address { get; set; }
		[Required]
		public int City { get; set; }
		public int State { get; set; }
		public int Province { get; set; }

		public string? CountryCode1 { get; set; }

		public string? LocationMobileNo1 { get; set; }
		public string? CountryCode2 { get; set; }

		public string? LocationMobileNo2 { get; set; }
		[Required]
		public int Country { get; set; }
		public string? CurrencyCode { get; set; } = "";
		public string? PostalCode { get; set; }
		public int CreatedBy { get; set; }
		public DateTime CreatedDate { get; set; }
		public int LastModeifiedBy { get; set; }
		public DateTime LastModeifiedDate { get; set; }
		[NotMapped]

		public string? CityName { get; set; }

		[NotMapped]

		public string? CountryName { get; set; }

		[NotMapped]

		public string? FullName { get; set; }

	}

	public class CountryMaster
	{
		[Key]
		public int ID { get; set; }

		
		public string? Name { get; set; }

		
		public string? CountryCode { get; set; }

		public string? Nationality { get; set; }

		public int? MobileCode { get; set; }

		public string? Slug { get; set; }

		public string? Currency { get; set; }
	}
}
