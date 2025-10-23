namespace Dict.Service.IService
{
    public interface IBlobService
    {
        Task<string> UploadFileBlobAsync(string containerName, Stream content, string contentType, string fileName);
        Task DeleteFileBlobAsync(string containerName, string blobName);
    }
}
