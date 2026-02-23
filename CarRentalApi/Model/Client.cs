using System.ComponentModel.DataAnnotations;

namespace CarRentalApi.Model
{
    public class Client
    {
        [Key]
        public int ClientId { get; set; }
        public int ? BranchId { get; set; }
        public string ? FirstName { get; set; }
        public string ? LastName { get; set; }
        public string ? Email { get; set; }
        public string ? Mobile { get; set; }
        public DateTime ? Date { get; set; } = DateTime.Now;
        public string ? ReferedBy { get; set; }
        public string ? BusinessProposal { get; set; }
        public string ? CompanyName { get; set; }
        public string ? CompanyAddress { get; set; }
        public string ? ComapanyType { get; set; }
        public string ? Designation { get; set; }
        public int? CreatedBy { get; set; } 
        public string? CreatedByName { get; set; }
        public bool ? StatusFlag { get; set; }

    }
}
