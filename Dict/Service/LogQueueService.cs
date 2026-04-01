using Dict.Models;
using System.Threading.Channels;

namespace Dict.Service
{
    public class LogQueueService
    {
        // Tạo một đường ống không giới hạn số lượng phần tử (Unbounded)
        private readonly Channel<ApiCall> _queue = Channel.CreateUnbounded<ApiCall>();

        // Hàm để ném Log vào ống
        public async ValueTask QueueLogAsync(ApiCall log)
        {
            await _queue.Writer.WriteAsync(log);
        }

        // Hàm để lấy Log ra khỏi ống
        public IAsyncEnumerable<ApiCall> DequeueAllAsync(CancellationToken ct)
        {
            return _queue.Reader.ReadAllAsync(ct);
        }
    }
}
