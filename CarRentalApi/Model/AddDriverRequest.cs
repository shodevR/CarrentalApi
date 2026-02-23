namespace CarRentalApi.Model
{
    public class AddDriverRequest
    {
        public Driver Driver { get; set; }
        public List<IFormFile> Files { get; set; }
        public List<DriverDocuments> DriverDocuments { get; set; }
    }

}
