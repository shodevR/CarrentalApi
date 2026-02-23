using System.ComponentModel.DataAnnotations;

namespace CarRentalApi.Model
{
    public class DriverADO
    {
        public int DriverDocumentsId { get; set; }  // Ensure this matches the column in your database
        public int? DriverId {  get; set; }
        public string Name { get; set; }
        public string? DocumentPath { get; set; }
        public DateTime UploadDate { get; set; }
        public DateTime? ExpireDate { get; set; }
      

    }
   
}
