using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Dict.Service.IService;

namespace Dict.Service
{
    public class BlobService : IBlobService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly IConfiguration _configuration;

        public BlobService(IConfiguration configuration)
        {
            _configuration = configuration;
            _blobServiceClient = new BlobServiceClient(_configuration["AzureStorage:ConnectionString"]);
        }

        public async Task<string> UploadFileBlobAsync(string containerName, Stream content, string contentType, string fileName)
        {
            // 1. Lấy container (tạo nếu chưa có)
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            // 2. Lấy tham chiếu đến blob (file)
            var blobClient = containerClient.GetBlobClient(fileName);

            // 3. Tải file lên
            await blobClient.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType });

            // 4. Trả về URL của file đã tải lên
            return blobClient.Uri.ToString();
        }
    }
}
