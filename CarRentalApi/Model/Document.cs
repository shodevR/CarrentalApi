using System;
using System.ComponentModel.DataAnnotations;

namespace CarRentalApi.Model
{
    public class Document
    {
        [Key]
        public int DocumentId { get; set; }

        [Required]
        public string Name { get; set; }

        public string DocumentPath { get; set; }  // Removed [Required] here

        // Optional field for vehicle ID to associate the document with a vehicle
        public int? VehicleId { get; set; }

        // Set a default value for UploadDate, which will be overridden in the controller if needed
        public DateTime UploadDate { get; set; } = DateTime.Now;
        public DateTime? ExpireDate { get; set; }
        public bool? IsMailSent { get; set; }  = false;
    }
}
