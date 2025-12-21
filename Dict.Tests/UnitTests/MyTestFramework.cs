using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;

// 1. Chỉ định cho xUnit dùng "bộ khung" tùy chỉnh của chúng ta
//    (Thay TenProjectTest bằng namespace của project test)
[assembly: TestFramework("Dict.Tests.UnitTests.MyTestFramework", "Dict.Tests")]
// (Giả sử namespace là Dict.Tests.UnitTests và tên assembly là Dict.Tests)

namespace Dict.Tests.UnitTests // <-- Đổi thành namespace của bạn
{
    // 2. Bộ khung tùy chỉnh, kế thừa XunitTestFramework
    public class MyTestFramework : XunitTestFramework
    {
        public MyTestFramework(IMessageSink messageSink) : base(messageSink) { }

        protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
            => new MyTestFrameworkExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
    }

    // 3. Bộ thực thi (Executor) tùy chỉnh
    public class MyTestFrameworkExecutor : XunitTestFrameworkExecutor
    {
        public MyTestFrameworkExecutor(AssemblyName assemblyName, ISourceInformationProvider sourceInfoProvider, IMessageSink diagnosticMessageSink)
            : base(assemblyName, sourceInfoProvider, diagnosticMessageSink)
        { }

        // 4. Override hàm RunTestCases để "lắng nghe" kết quả cuối cùng
        protected override async void RunTestCases(IEnumerable<IXunitTestCase> testCases, IMessageSink sink, ITestFrameworkExecutionOptions executionOptions)
        {
            // === Đây là nơi GlobalTestHook được "cài" vào ===
            // 5. Khởi tạo Hook (chạy InitializeAsync)
            var hook = new GlobalTestHook();
            await hook.InitializeAsync();

            // 6. Chạy tất cả các test
            using (var assemblyRunner = new XunitTestAssemblyRunner(TestAssembly, testCases, DiagnosticMessageSink, sink, executionOptions))
            {
                var assemblyRunnerResult = await assemblyRunner.RunAsync();

                // 7. (QUAN TRỌNG) Bắt kết quả và cập nhật biến static
                if (assemblyRunnerResult.Failed > 0)
                {
                    GlobalTestHook.SomethingFailed = true;
                }
            }

            // 8. Chạy hàm dọn dẹp (chạy DisposeAsync - tạo báo cáo)
            await hook.DisposeAsync();
        }
    }
}