using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Dict.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        // Track viewers per document: documentId → { userId → JSON(userName,avatarUrl) }
        private static readonly ConcurrentDictionary<int, ConcurrentDictionary<string, (string UserName, string? AvatarUrl)>> _documentViewers = new();
        // Track which document each connectionId is in (for cleanup on disconnect)
        private static readonly ConcurrentDictionary<string, (int DocId, string UserId)> _connectionMap = new();

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Tự cleanup khi mất kết nối đột ngột (đóng tab, tắt mạng)
            if (_connectionMap.TryRemove(Context.ConnectionId, out var info))
            {
                if (_documentViewers.TryGetValue(info.DocId, out var viewers))
                {
                    viewers.TryRemove(info.UserId, out _);
                    if (viewers.IsEmpty) _documentViewers.TryRemove(info.DocId, out _);
                }
                await Clients.OthersInGroup($"Document_{info.DocId}").SendAsync("UserLeft", info.UserId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        // =========================================================
        // PHẦN DÀNH CHO OCR — REALTIME PROGRESS
        // =========================================================

        /// <summary>Frontend gọi khi bắt đầu chờ OCR job — nhận event OcrCompleted / OcrPageCompleted</summary>
        public async Task JoinOcrRoom(int jobId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"OcrJob_{jobId}");
        }

        public async Task LeaveOcrRoom(int jobId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"OcrJob_{jobId}");
        }

        // =========================================================
        // PHẦN DÀNH CHO COMMENT FILE PDF THỜI GIAN THỰC
        // =========================================================

        /// <summary>Frontend gọi khi mở trang đọc PDF (/reader)</summary>
        public async Task JoinDocumentRoom(int documentId, string? avatarUrl = null)
        {
            string roomName = $"Document_{documentId}";
            // Luôn add vào group trước — dù các bước sau có fail, client vẫn nhận được broadcasts
            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);

            try
            {
                var userId = Context.UserIdentifier ?? Context.ConnectionId;
                var userName = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                            ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                            ?? userId;

                // Lưu vào tracker — dùng named tuple để đảm bảo type nhất quán
                var viewers = _documentViewers.GetOrAdd(documentId, _ => new ConcurrentDictionary<string, (string UserName, string? AvatarUrl)>());
                viewers[userId] = (userName, avatarUrl);
                _connectionMap[Context.ConnectionId] = (documentId, userId);

                // Gửi danh sách hiện tại cho người vừa join
                var currentList = viewers.Select(kv => new { UserId = kv.Key, kv.Value.UserName, kv.Value.AvatarUrl }).ToList();
                await Clients.Caller.SendAsync("RoomViewers", currentList);

                // Notify mọi người khác
                await Clients.OthersInGroup(roomName).SendAsync("UserJoined", new { UserId = userId, UserName = userName, AvatarUrl = avatarUrl });
            }
            catch (Exception ex)
            {
                // Không throw — client đã join group thành công, chỉ thiếu viewer list
                Console.Error.WriteLine($"[NotificationHub] JoinDocumentRoom metadata error (doc {documentId}): {ex.Message}");
            }
        }

        /// <summary>Frontend gọi khi đóng file / tắt trang</summary>
        public async Task LeaveDocumentRoom(int documentId)
        {
            string roomName = $"Document_{documentId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);

            var userId = Context.UserIdentifier ?? Context.ConnectionId;
            if (_documentViewers.TryGetValue(documentId, out var viewers))
            {
                viewers.TryRemove(userId, out _);
                if (viewers.IsEmpty) _documentViewers.TryRemove(documentId, out _);
            }
            _connectionMap.TryRemove(Context.ConnectionId, out _);

            await Clients.OthersInGroup(roomName).SendAsync("UserLeft", userId);
        }

        // =========================================================
        // PHẦN DÀNH CHO CON TRỎ CỘNG TÁC (COLLABORATIVE CURSOR)
        // =========================================================

        /// <summary>
        /// Frontend gọi mỗi khi di chuyển chuột trên PDF (throttle 50ms).
        /// Broadcast toạ độ % đến tất cả user khác trong cùng document room.
        /// </summary>
        public async Task BroadcastCursor(int documentId, double xPct, double yPct, int page)
        {
            // UserIdentifier = userId (số) từ EmailBasedUserIdProvider
            // sub claim = username để hiển thị
            var userId = Context.UserIdentifier ?? Context.ConnectionId;
            // ASP.NET Core JWT remaps "sub" → ClaimTypes.NameIdentifier (không phải "sub" string)
            var userName = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                        ?? userId;

            var payload = new
            {
                UserId = userId,
                UserName = userName,
                XPct = xPct,
                YPct = yPct,
                Page = page,
            };

            await Clients.OthersInGroup($"Document_{documentId}")
                         .SendAsync("CursorMoved", payload);
        }

        /// <summary>Frontend gọi khi user rời tab PDF — xoá cursor khỏi màn hình người khác.</summary>
        public async Task LeaveCursor(int documentId)
        {
            var userId = Context.UserIdentifier ?? Context.ConnectionId;
            await Clients.OthersInGroup($"Document_{documentId}")
                         .SendAsync("CursorLeft", userId);
        }


        // =========================================================
        // PHẦN DÀNH CHO VẼ CỘNG TÁC (COLLABORATIVE ANNOTATION)
        // =========================================================

        /// <summary>
        /// Frontend gọi mỗi khi hoàn thành một nét vẽ (pointerup).
        /// Broadcast stroke JSON đến tất cả user khác trong cùng document room.
        /// </summary>
        public async Task BroadcastAnnotationStroke(int documentId, int pageNumber, string strokeJson)
        {
            var userId = Context.UserIdentifier ?? Context.ConnectionId;
            var payload = new { UserId = userId, PageNumber = pageNumber, StrokeJson = strokeJson };
            await Clients.OthersInGroup($"Document_{documentId}")
                         .SendAsync("AnnotationStroke", payload);
        }

        /// <summary>
        /// Frontend gọi sau khi dùng tẩy xong (pointerup với eraser).
        /// Broadcast toàn bộ danh sách strokes còn lại của trang đó.
        /// </summary>
        public async Task BroadcastAnnotationErase(int documentId, int pageNumber, string strokesJson)
        {
            var userId = Context.UserIdentifier ?? Context.ConnectionId;
            var payload = new { UserId = userId, PageNumber = pageNumber, StrokesJson = strokesJson };
            await Clients.OthersInGroup($"Document_{documentId}")
                         .SendAsync("AnnotationErased", payload);
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