using System.Threading.Channels;

namespace Dict.Service
{
    public class LogQueueService
    {
        // ĐỔI TỪ Channel<ApiCall> SANG Channel<object> để chứa được vạn vật
        private readonly Channel<object> _queue = Channel.CreateUnbounded<object>(new UnboundedChannelOptions
        {
            SingleReader = true, // Bật cái này lên để tối ưu tốc độ nếu chỉ có 1 Worker đọc
            SingleWriter = false
        });

        // Hàm ném hàng vào ống (Đổi tham số thành object)
        public async ValueTask QueueLogAsync(object log)
        {
            await _queue.Writer.WriteAsync(log);
        }

        // Hàm lấy hàng ra (Trả về object)
        public IAsyncEnumerable<object> DequeueAllAsync(CancellationToken ct)
        {
            return _queue.Reader.ReadAllAsync(ct);
        }
    }
}