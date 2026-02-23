using System.ComponentModel.DataAnnotations;

namespace CarRentalApi.Model
{
    public class CheckList
    {
        [Key]
        public int CheckListId { get; set; }
        public int BookingId { get; set; }
        public int  BranchId { get; set; }
        public int VehicleId { get; set; }
        public int OdometerBefore { get; set; }
        public int OdometerAfter { get; set; }
        public int FuelBefore { get; set; }
        public int FuelAfter { get; set; }
        public int TyrePressureBefore { get; set; }
        public double? ExtraHours { get; set; } = 0;
        public int TyrePressureAfter { get; set; }
        public string CarToolsBefore { get; set; }
        public string CarToolsAfter { get; set; }
        public string SafetyEquipmentsBefore { get; set; }
        public string SafetyEquipmentsAfter { get; set; }
        public DateTime DateOfReadingBefore { get; set; }
        public DateTime DateOfReadingAfter { get; set; }
        public int? CreatedByBefore { get; set; }
        public int? CreatedByAfter { get; set; }
        public bool StatusFlag { get; set; }
    }
}
