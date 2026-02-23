using System.ComponentModel.DataAnnotations;

namespace CarRentalApi.Model
{
    public class VehicleDocument
    {
        [Key]
        public int DocumentId { get; set; }
        [Required]
        public int VehicleId { get; set; }
        public string LicensePlate { get; set; }
        public string LicensePlateExp { get; set; }
        public string RegistratingPapers { get; set; }
        public string RegistratingPapersExp { get; set; }
        public string Insurance { get; set; }
        public string InsuranceExp { get; set; }
        public string MaintenanceReceipts { get; set; }
        public string MaintenanceReceiptsExp { get; set; }
        public string OtherDocs { get; set; }
        public string OtherDocsExp { get; set; }
        public bool StatusFlag { get; set; }
        
    }
}
