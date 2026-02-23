namespace CarRentalApi.Service
{
    public interface IImageUploadService
    {
        Task<string> UploadImageAsync(IFormFile file);
       
    }
}
