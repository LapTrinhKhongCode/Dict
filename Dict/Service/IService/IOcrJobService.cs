using Dict.DTO.OCR;

public interface IOcrJobService
{
    Task<OcrJobDto> CreateAsync(OcrJobCreateDto createDto);
    Task AppendResultsAsync(int jobId, List<CreateOcrResultDto> results);
    Task UpdateStatusAsync(int jobId, OcrJobUpdateStatusDto updateDto);

    // 🆕
    Task AppendDetectedTextAsync(int jobId, string newText);
    Task TryCompleteJobAsync(int jobId, int totalPages);
}