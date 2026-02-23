namespace CarRentalApi.Service
{
    public class ImageUploadService : IImageUploadService
    {
        private readonly string _sasUrl;

        public ImageUploadService(string sasUrl)
        {
            _sasUrl = sasUrl ?? throw new ArgumentNullException(nameof(sasUrl));
        }

        public string RootPath => _sasUrl;

        public async Task<string> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File cannot be empty.");
            }

            try
            {
                // Create a BlobServiceClient
                var blobServiceClient = new Azure.Storage.Blobs.BlobServiceClient(new Uri(_sasUrl));
                var containerClient = blobServiceClient.GetBlobContainerClient("cbt-car-rental");

                // Generate a random file name
                var blobName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var blobClient = containerClient.GetBlobClient(blobName);

                // Upload the file to Azure Blob Storage
                using (var stream = file.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }

                // Return the blob's clean URL (without query string)
                var fullUrl = blobClient.Uri.ToString();
                var cleanUrl = fullUrl.Contains('?') ? fullUrl.Substring(0, fullUrl.IndexOf('?')) : fullUrl;

                return cleanUrl;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error uploading the file to Azure Blob Storage.", ex);
            }
        }

    }
}
