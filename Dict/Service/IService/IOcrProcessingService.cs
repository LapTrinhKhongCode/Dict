using Dict.DTO.OCR;

namespace Dict.Service.IService
{
    public interface IOcrProcessingService
    {
        Task<OcrProcessingResultDto> ProcessImageAsync(IFormFile image, int userId, bool saveAnnotated);
        Task<IEnumerable<OcrJobDetailDto>> GetRecentOcrJobsForUserAsync(int userId, int limit = 5);
    }
}

