using Dict.Data;
using Dict.DTO;
using Dict.Enums;
using Dict.Models;
using Dict.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Dict.Service.IService;

namespace Dict.Services
{
    public class WorkspaceInvitationService : IWorkspaceInvitationService
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHubContext<NotificationHub> _hubContext; // Thêm Hub Context

        // Inject IHubContext vào constructor
        public WorkspaceInvitationService(ApplicationDbContext db, IHubContext<NotificationHub> hubContext, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _hubContext = hubContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<WorkspaceInvitationDTO> InviteMemberAsync(int inviterId, CreateInvitationDTO dto)
        {
            // 0. Tìm người dùng trong hệ thống dựa trên Email HOẶC Username
            var invitee = await _db.Users.FirstOrDefaultAsync(u =>
                u.Email == dto.InviteeIdentifier || u.UserName == dto.InviteeIdentifier);

            if (invitee == null)
            {
                throw new Exception("Không tìm thấy người dùng nào đăng ký với email hoặc username này.");
            }

            // (Bảo mật thêm) Không cho phép tự gửi lời mời cho chính mình
            if (invitee.Id == inviterId)
            {
                throw new Exception("Bạn không thể tự mời chính mình vào dự án.");
            }

            // (Bảo mật thêm) Kiểm tra xem người này đã là thành viên của Workspace chưa
            var isAlreadyMember = await _db.WorkspaceMembers
                .AnyAsync(wm => wm.WorkspaceId == dto.WorkspaceId && wm.UserId == invitee.Id);
            if (isAlreadyMember)
            {
                throw new Exception("Người này hiện đã là thành viên của Workspace.");
            }

            // 1. Kiểm tra xem đã gửi lời mời đang chờ duyệt chưa
            var exists = await _db.WorkspaceInvitations
                .AnyAsync(x => x.WorkspaceId == dto.WorkspaceId && x.InviteeId == invitee.Id && x.Status == InvitationStatus.PENDING);
            if (exists)
            {
                throw new Exception("Người này đã có lời mời đang chờ duyệt.");
            }

            // 2. Lưu vào Database (Xử lý Offline - Persistent)
            var invite = new WorkspaceInvitation
            {
                WorkspaceId = dto.WorkspaceId,
                InviteeId = invitee.Id,
                InviterId = inviterId,
                ExpectedRole = dto.ExpectedRole,
                Status = InvitationStatus.PENDING
            };

            _db.WorkspaceInvitations.Add(invite);
            await _db.SaveChangesAsync();

            // 3. Chuẩn bị DTO để trả về và gửi SignalR
            var workspace = await _db.Workspaces.FindAsync(dto.WorkspaceId);
            var inviter = await _db.Users.FindAsync(inviterId);

            var resultDto = new WorkspaceInvitationDTO
            {
                Id = invite.Id,
                WorkspaceId = invite.WorkspaceId,
                WorkspaceName = workspace?.Name ?? "Workspace",
                InviterName = inviter?.UserName ?? "ADMIN",
                ExpectedRole = invite.ExpectedRole,
                Status = invite.Status.ToString(),
                CreatedAt = invite.CreatedAt
            };

            // 4. Bắn SignalR (Xử lý Online - Realtime)
            // EmailBasedUserIdProvider dùng claim "userId" (số nguyên) làm key,
            // nên phải truyền userId.ToString() — KHÔNG dùng email.
            await _hubContext.Clients.User(invitee.Id.ToString())
                .SendAsync("ReceiveNewInvitation", resultDto);

            return resultDto;
        }
        public async Task<IEnumerable<WorkspaceInvitationDTO>> GetMyPendingInvitationsAsync(int userid)
        {
            var userId = int.Parse(_httpContextAccessor.HttpContext.User.FindFirst("userId")?.Value);

            return await _db.WorkspaceInvitations
                .Include(x => x.Workspace)
                .Include(x => x.Inviter)
                .Where(x => x.InviteeId == userId && x.Status == InvitationStatus.PENDING)
                .Select(x => new WorkspaceInvitationDTO
                {
                    Id = x.Id,
                    WorkspaceId = x.WorkspaceId,
                    WorkspaceName = x.Workspace.Name,
                    InviterName = x.Inviter.UserName,
                    ExpectedRole = x.ExpectedRole,
                    Status = x.Status.ToString(),
                    CreatedAt = x.CreatedAt
                }).ToListAsync();
        }
        public async Task<bool> RespondToInvitationAsync(int userId, int invitationId, bool isAccepted)
        {
            var invite = await _db.WorkspaceInvitations
        .Include(x => x.Workspace)
        .Include(x => x.Invitee)
        .Include(x => x.Inviter) // 1. Phải Include thêm Inviter để lấy Email
        .FirstOrDefaultAsync(x => x.Id == invitationId && x.InviteeId == userId);

            if (invite == null || invite.Status != InvitationStatus.PENDING) return false;

            if (isAccepted)
            {
                invite.Status = InvitationStatus.ACCEPTED;
                _db.WorkspaceMembers.Add(new WorkspaceMember
                {
                    WorkspaceId = invite.WorkspaceId,
                    UserId = userId,
                    Role = invite.ExpectedRole
                });
            }
            else
            {
                invite.Status = InvitationStatus.DECLINED;
            }

            await _db.SaveChangesAsync();

            var inviterIdStr = invite.InviterId.ToString();
            var responseMessage = isAccepted
                ? $"{invite.Invitee.UserName} đã CHẤP NHẬN tham gia {invite.Workspace.Name}."
                : $"{invite.Invitee.UserName} đã TỪ CHỐI tham gia {invite.Workspace.Name}.";

            // 3. Gửi qua User dùng userId (số nguyên) — phải khớp với EmailBasedUserIdProvider
            await _hubContext.Clients.User(inviterIdStr)
                             .SendAsync("InvitationResponded", new
                             {
                                 InvitationId = invitationId,
                                 WorkspaceId = invite.WorkspaceId,
                                 IsAccepted = isAccepted,
                                 Message = responseMessage
                             });

            return true;
        }
    }
}