using Xunit;

// Thêm dòng này vào bất kỳ file C# nào trong project test
// (Tạo file riêng cho sạch sẽ)
// Dòng này ra lệnh cho xUnit TẮT chạy test song song cho toàn bộ project
[assembly: CollectionBehavior(DisableTestParallelization = true)]