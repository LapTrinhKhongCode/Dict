using Dict.DTO.OCR;

namespace Dict.Service.IService
{
    public interface IOcrJobService
    {
        /// <summary>
        /// Tạo một OcrJob mới và trả về DTO của nó.
        /// </summary>
        Task<OcrJobDto> CreateAsync(OcrJobCreateDto createDto);

        /// <summary>
        /// Cập nhật trạng thái và/hoặc văn bản đã phát hiện của một job.
        /// </summary>
        Task UpdateStatusAsync(int jobId, OcrJobUpdateStatusDto updateDto);

        /// <summary>
        /// Thêm một danh sách các kết quả (từ/dòng) vào một OcrJob đã có.
        /// </summary>
        Task AppendResultsAsync(int jobId, List<CreateOcrResultDto> results);
    }
}
