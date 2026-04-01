using Dict.Data;
using Dict.DTO;
using Dict.Hubs;
using Dict.Models;
using Dict.Service.IService;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Dict.Services
{
    public class FileCommentService : IFileCommentService
    {
        private readonly ApplicationDbContext _db;
        private readonly IHubContext<NotificationHub> _hubContext; // Khai báo HubContext

        // Bơm IHubContext vào thông qua Constructor
        public FileCommentService(ApplicationDbContext db, IHubContext<NotificationHub> hubContext)
        {
            _db = db;
            _hubContext = hubContext;
        }
        public async Task<FileCommentDTO> AddCommentAsync(int userId, CreateCommentDTO dto)
        {
            var comment = new FileComment
            {
                UserId = userId,
                MediaStoreId = dto.MediaStoreId,
                ParentCommentId = dto.ParentCommentId,
                Content = dto.Content,
                PageNumber = dto.PageNumber,
                AnnotationData = dto.AnnotationData,
                CreatedAt = DateTime.UtcNow
            };

            _db.FileComments.Add(comment);
            await _db.SaveChangesAsync();

            // 1. Lấy thông tin User để trả về DTO cho đẹp (Có tên người comment)
            var user = await _db.Users.FindAsync(userId);

            var resultDto = new FileCommentDTO
            {
                Id = comment.Id,
                UserId = userId,
                UserName = user?.UserName ?? "Thành viên", // Dự phòng nếu null
                ParentCommentId = comment.ParentCommentId,
                Content = comment.Content,
                PageNumber = comment.PageNumber,
                AnnotationData = comment.AnnotationData,
                CreatedAt = comment.CreatedAt,
                IsDeleted = false
            };

            // 2. Bắn SignalR thông báo CÓ COMMENT MỚI
            // Chỉ gửi cho những người đang ở trong Room của Document này
            string roomName = $"Document_{dto.MediaStoreId}";
            await _hubContext.Clients.Group(roomName).SendAsync("ReceiveNewComment", resultDto);

            return resultDto; // Trả về DTO đầy đủ
        }

        public async Task<IEnumerable<FileCommentDTO>> GetCommentsByFileAsync(int mediaStoreId)
        {
            // Lấy comment gốc (không có parent) kèm theo các reply (đệ quy 1 cấp)
            var comments = await _db.FileComments
                .Include(c => c.User)
                .Include(c => c.Replies)
                    .ThenInclude(r => r.User)
                .Where(c => c.MediaStoreId == mediaStoreId && c.ParentCommentId == null)
                .Select(c => new FileCommentDTO
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    UserName = c.User.UserName,
                    Content = c.IsDeleted ? "Bình luận này đã bị xóa." : c.Content, // Xử lý UI
                    PageNumber = c.PageNumber,
                    AnnotationData = c.AnnotationData,
                    IsDeleted = c.IsDeleted,
                    CreatedAt = c.CreatedAt,
                    Replies = c.Replies.Select(r => new FileCommentDTO
                    {
                        Id = r.Id,
                        UserId = r.UserId,
                        UserName = r.User.UserName,
                        Content = r.IsDeleted ? "Bình luận này đã bị xóa." : r.Content,
                        CreatedAt = r.CreatedAt
                    }).OrderBy(r => r.CreatedAt).ToList()
                })
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            return comments;
        }

        public async Task<bool> DeleteCommentAsync(int userId, int commentId)
        {
            var comment = await _db.FileComments.FirstOrDefaultAsync(c => c.Id == commentId && c.UserId == userId);
            if (comment == null) return false;

            comment.IsDeleted = true; // Xóa mềm
            await _db.SaveChangesAsync();

            // Bắn SignalR thông báo COMMENT BỊ XÓA
            // Gửi ID của comment bị xóa để Frontend tự update UI
            string roomName = $"Document_{comment.MediaStoreId}";
            await _hubContext.Clients.Group(roomName).SendAsync("CommentDeleted", commentId);

            return true;
        }     
    }
}