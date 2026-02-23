using System.ComponentModel.DataAnnotations;

namespace CarRentalApi.Model
{
    public class VehicleType
    {
        [Key]
        public int Id { get; set; }
        public string TypeName { get; set; }

    }

}
