using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

// Class này sẽ là "hook" toàn cục
public class GlobalTestHook : IAsyncLifetime
{
    // Biến này để theo dõi trạng thái
    // (Đây là một mẹo nhỏ để ghi lại nếu CÓ lỗi xảy ra)
    public static bool SomethingFailed = false;

    public Task InitializeAsync()
    {
        // Code chạy TRƯỚC KHI tất cả test bắt đầu
        // Ví dụ: Xóa file log cũ
        if (File.Exists("TestReport.log"))
        {
            File.Delete("TestReport.log");
        }
        File.AppendAllText("TestReport.log", $"=== Test Run BẮT ĐẦU lúc: {DateTime.Now} ===\n");
        SomethingFailed = false; // Reset
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        // Code chạy SAU KHI tất cả test kết thúc
        File.AppendAllText("TestReport.log", $"\n=== Test Run KẾT THÚC lúc: {DateTime.Now} ===\n");

        if (SomethingFailed)
        {
            File.AppendAllText("TestReport.log", "TRẠNG THÁI: CÓ LỖI XẢY RA!\n");
        }
        else
        {
            // Đây là nơi gần nhất với yêu cầu của bạn
            File.AppendAllText("TestReport.log", "TRẠNG THÁI: TẤT CẢ ĐỀU PASS (hoặc được skip).\n");

            // Bạn có thể tạo file "thành công" ở đây
            File.WriteAllText("AllTestsPassed.txt", "OK");
        }

        return Task.CompletedTask;
    }
}

// (Tùy chọn) Tạo một class cơ sở để các test có thể cập nhật trạng thái
public class BaseTest : IDisposable
{
    protected readonly ITestOutputHelper output; // Để ghi log (nếu cần)

    public BaseTest(ITestOutputHelper output)
    {
        this.output = output;
    }

    public void Dispose()
    {
        // Nếu test này bị lỗi, nó sẽ cập nhật biến global
        // (Cách này không hoàn hảo 100% nhưng là một mẹo)
        // (Bạn cần một cách tốt hơn để bắt lỗi, ví dụ dùng try/catch trong test)
    }
}