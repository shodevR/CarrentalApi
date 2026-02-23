using System.ComponentModel.DataAnnotations;

namespace CarRentalApi.Model
{
    public class DriverDocument
    {
        [Key]
        public int DriverDocuId { get; set; }
        public int DriverId { get; set; }
        public string LicensePlate { get; set; }
        public DateTime LicenseExpDate { get; set; }
        public string NationalId {  get; set; }
        public DateTime NationalIdExpDate { get; set; }
        public string OtherDocument { get; set; }
        public DateTime OtherDocumentExpDate { get; set; }
        public string upload { get; set; }

    }
}
