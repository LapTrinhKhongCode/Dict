using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Dict.Hubs
{
    [Authorize] // Chỉ cho phép user đã đăng nhập kết nối
    public class NotificationHub : Hub
    {
        // 1. Chạy khi User vừa mở app và kết nối thành công tới Hub
        public override async Task OnConnectedAsync()
        {
            Console.WriteLine("UserIdentifier: " + Context.UserIdentifier);

            foreach (var c in Context.User.Claims)
            {
                Console.WriteLine($"{c.Type} = {c.Value}");
            }

            await base.OnConnectedAsync();
        }

        // 2. Chạy khi User đóng tab, tắt app hoặc rớt mạng
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            // TODO: Cập nhật trạng thái "Offline" hoặc ghi log thời gian hoạt động cuối cùng

            await base.OnDisconnectedAsync(exception);
        }

        // =========================================================
        // PHẦN DÀNH CHO COMMENT FILE PDF THỜI GIAN THỰC
        // =========================================================

        /// <summary>
        /// Frontend gọi hàm này khi mở trang đọc PDF (/reader)
        /// </summary>
        public async Task JoinDocumentRoom(int mediaStoreId)
        {
            string roomName = $"Document_{mediaStoreId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        }

        /// <summary>
        /// Frontend gọi hàm này khi thoát trang đọc PDF
        /// </summary>
        public async Task LeaveDocumentRoom(int mediaStoreId)
        {
            string roomName = $"Document_{mediaStoreId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
        }

        // =========================================================
        // PHẦN DÀNH CHO HOẠT ĐỘNG CHUNG CỦA WORKSPACE
        // =========================================================

        /// <summary>
        /// Frontend gọi khi user chọn một Workspace làm Active Workspace
        /// </summary>
        public async Task JoinWorkspaceRoom(int workspaceId)
        {
            string roomName = $"Workspace_{workspaceId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        }

        public async Task LeaveWorkspaceRoom(int workspaceId)
        {
            string roomName = $"Workspace_{workspaceId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
        }
    }
}