using Dict.DTO.OCR;

namespace Dict.Service.IService
{
    public interface IOcrProcessingService
    {
        Task<OcrProcessingResultDto> UploadImageOnlyAsync(IFormFile image, int userId, int workspaceId, int? projectId);
        Task<OcrProcessingResultDto> ProcessOcrLazyAsync(int jobId);
        Task<OcrProcessingResultDto> ProcessImageAsync(IFormFile image, int userId, int workspaceId, int? projectId, bool saveAnnotated);
        Task<IEnumerable<OcrJobDetailDto>> GetRecentOcrJobsForUserAsync(int userId, int limit = 5);

    }
}

