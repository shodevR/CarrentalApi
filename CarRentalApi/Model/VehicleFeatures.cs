using System.ComponentModel.DataAnnotations;

namespace CarRentalApi.Model
{
    public class VehicleFeatures
    {
        [Key]
        public int VehicleFeatureId { get; set; }
        public string? VehicleFeatureName { get; set; }


    }
}
