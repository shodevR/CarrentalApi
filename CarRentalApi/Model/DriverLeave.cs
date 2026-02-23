using Microsoft.Extensions.Primitives;
using System.ComponentModel.DataAnnotations;

namespace CarRentalApi.Model
{
    public class DriverLeave
    {
        [Key]
        public int DriverLeaveId { get; set; }
        public int DriverId { get; set; }
        public string DriverName { get; set; }
        public DateTime LeaveDateFrom { get; set; }
        public DateTime LeaveDateTo { get; set; }
        public string Reason { get; set; }
        public string ApprovedBy { get; set; }
        public int BranchId { get; set; }
        public int? CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
        public bool StatusFlag { get; set; }

    }
}
