namespace CarRentalApi.Model
{
    public class BaseModify
    {
       
        public string? ModifiedRequestedBy { get; set; }
        public string? ModifiedReason { get; set; }
        public bool? ModifiedStatus { get; set; } // 0 - pending, 1 - approved, 2 - rejected
        public DateTime? ModifiedDate { get; set; } = DateTime.Now;
        public int? ModifiedBy { get; set; }
        public string? ModifiedByName { get; set; }

    }
}
