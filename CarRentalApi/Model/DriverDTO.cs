namespace CarRentalApi.Model
{
    public class DriverDTO : Driver
    {
        public List<DriverADO> Documents { get; set; }
        public List<IFormFile>? Files { get; set; }
       
    }
}
