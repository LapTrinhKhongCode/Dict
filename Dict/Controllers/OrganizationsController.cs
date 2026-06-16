using Dict.Data;
using Dict.DTO;
using Dict.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dict.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrganizationsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public OrganizationsController(ApplicationDbContext db)
        {
            _db = db;
        }

        private int GetUserId()
        {
            var c = User.FindFirst("userId");
            if (c == null || !int.TryParse(c.Value, out var id)) throw new Exception("Invalid user");
            return id;
        }

        // ── GET /api/organizations/my ─────────────────────────────────────
        /// <summary>Danh sách organizations user đang tham gia</summary>
        [HttpGet("my")]
        public async Task<IActionResult> GetMyOrgs()
        {
            var userId = GetUserId();
            var orgs = await _db.OrganizationMembers
                .Where(m => m.UserId == userId)
                .Include(m => m.Organization)
                .Select(m => new
                {
                    m.Organization.Id,
                    m.Organization.Name,
                    m.Organization.Slug,
                    m.Organization.OrgPlan,
                    m.Organization.MaxMembers,
                    myRole = m.OrgRole,
                    memberCount = m.Organization.Members.Count(),
                    m.Organization.CreatedAt,
                })
                .ToListAsync();

            return Ok(new ResponseDTO { Result = orgs });
        }

        // ── POST /api/organizations ───────────────────────────────────────
        /// <summary>Tạo organization mới — user tự động là OWNER</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrgDto dto)
        {
            var userId = GetUserId();

            var slug = dto.Name.ToLower()
                .Replace(" ", "-")
                .Replace("_", "-");

            var org = new Organization
            {
                Name = dto.Name,
                Slug = slug,
                Description = dto.Description,
                OrgPlan = OrgPlan.FREE,
                MaxMembers = 5,
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow,
            };
            _db.Organizations.Add(org);
            await _db.SaveChangesAsync();

            // Thêm creator là OWNER
            _db.OrganizationMembers.Add(new OrganizationMember
            {
                OrganizationId = org.Id,
                UserId = userId,
                OrgRole = OrgRole.OWNER,
                JoinedAt = DateTime.UtcNow,
            });
            await _db.SaveChangesAsync();

            return Ok(new ResponseDTO { Result = new { org.Id, org.Name, org.Slug, org.OrgPlan } });
        }

        // ── POST /api/organizations/{id}/members ──────────────────────────
        /// <summary>Mời user vào organization</summary>
        [HttpPost("{id}/members")]
        public async Task<IActionResult> InviteMember(int id, [FromBody] InviteOrgMemberDto dto)
        {
            var userId = GetUserId();

            // Check quyền: chỉ OWNER/ADMIN mới invite
            var myMembership = await _db.OrganizationMembers
                .FirstOrDefaultAsync(m => m.OrganizationId == id && m.UserId == userId);
            if (myMembership == null || myMembership.OrgRole == OrgRole.MEMBER)
                return Forbid();

            var org = await _db.Organizations.FindAsync(id);
            if (org == null) return NotFound();

            // Check member limit
            if (org.MaxMembers.HasValue)
            {
                var count = await _db.OrganizationMembers.CountAsync(m => m.OrganizationId == id);
                if (count >= org.MaxMembers.Value)
                    return BadRequest(new ResponseDTO
                    {
                        IsSuccess = false,
                        Message = $"Tổ chức đã đạt giới hạn {org.MaxMembers} thành viên. Nâng cấp plan để thêm."
                    });
            }

            var invitee = await _db.Users.FirstOrDefaultAsync(u =>
                u.Email == dto.Email || u.UserName == dto.Email);
            if (invitee == null)
                return NotFound(new ResponseDTO { IsSuccess = false, Message = "Không tìm thấy user với email hoặc username này." });

            var existing = await _db.OrganizationMembers
                .AnyAsync(m => m.OrganizationId == id && m.UserId == invitee.Id);
            if (existing)
                return Conflict(new ResponseDTO { IsSuccess = false, Message = "User đã là thành viên." });

            _db.OrganizationMembers.Add(new OrganizationMember
            {
                OrganizationId = id,
                UserId = invitee.Id,
                OrgRole = dto.Role ?? OrgRole.MEMBER,
                JoinedAt = DateTime.UtcNow,
            });
            await _db.SaveChangesAsync();

            // Auto-add vào tất cả workspace thuộc Org này (Slack model)
            var orgWorkspaces = await _db.Workspaces
                .Where(w => w.OrganizationId == id && w.OwnerType == "ORGANIZATION")
                .Select(w => w.Id)
                .ToListAsync();

            foreach (var wsId in orgWorkspaces)
            {
                var alreadyInWs = await _db.WorkspaceMembers
                    .AnyAsync(wm => wm.WorkspaceId == wsId && wm.UserId == invitee.Id);
                if (!alreadyInWs)
                {
                    _db.WorkspaceMembers.Add(new WorkspaceMember
                    {
                        WorkspaceId = wsId,
                        UserId = invitee.Id,
                        Role = WorkspaceRole.MEMBER
                    });
                }
            }
            if (orgWorkspaces.Any()) await _db.SaveChangesAsync();

            return Ok(new ResponseDTO { Message = $"Đã thêm {invitee.UserName} vào tổ chức và {orgWorkspaces.Count} workspace." });
        }

        // ── GET /api/organizations/{id}/members ───────────────────────────
        [HttpGet("{id}/members")]
        public async Task<IActionResult> GetMembers(int id)
        {
            var userId = GetUserId();
            var isMember = await _db.OrganizationMembers.AnyAsync(m => m.OrganizationId == id && m.UserId == userId);
            if (!isMember) return Forbid();

            var members = await _db.OrganizationMembers
                .Where(m => m.OrganizationId == id)
                .Include(m => m.User)
                .Select(m => new
                {
                    m.UserId,
                    m.User.UserName,
                    m.User.Email,
                    m.User.AvatarUrl,
                    m.OrgRole,
                    m.JoinedAt,
                })
                .ToListAsync();

            return Ok(new ResponseDTO { Result = members });
        }

        // ── DELETE /api/organizations/{id}/members/{memberId} ─────────────
        [HttpDelete("{id}/members/{memberId}")]
        public async Task<IActionResult> RemoveMember(int id, int memberId)
        {
            var userId = GetUserId();
            var myRole = await _db.OrganizationMembers
                .Where(m => m.OrganizationId == id && m.UserId == userId)
                .Select(m => m.OrgRole)
                .FirstOrDefaultAsync();

            if (myRole == null || myRole == OrgRole.MEMBER) return Forbid();
            if (memberId == userId) return BadRequest(new ResponseDTO { IsSuccess = false, Message = "Không thể xoá chính mình." });

            var target = await _db.OrganizationMembers
                .FirstOrDefaultAsync(m => m.OrganizationId == id && m.UserId == memberId);
            if (target == null) return NotFound();
            if (target.OrgRole == OrgRole.OWNER) return BadRequest(new ResponseDTO { IsSuccess = false, Message = "Không thể xoá Owner." });

            _db.OrganizationMembers.Remove(target);

            // Remove khỏi tất cả Org workspace (Slack model: leave Org = leave all Org workspaces)
            var orgWorkspaceIds = await _db.Workspaces
                .Where(w => w.OrganizationId == id && w.OwnerType == "ORGANIZATION")
                .Select(w => w.Id).ToListAsync();

            var wsMembers = await _db.WorkspaceMembers
                .Where(wm => orgWorkspaceIds.Contains(wm.WorkspaceId) && wm.UserId == memberId
                             && wm.Role != WorkspaceRole.OWNER)
                .ToListAsync();
            _db.WorkspaceMembers.RemoveRange(wsMembers);

            await _db.SaveChangesAsync();
            return Ok(new ResponseDTO { Message = "Đã xoá thành viên khỏi tổ chức và các workspace." });
        }

        // ── PUT /api/organizations/{id} ───────────────────────────────────
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateOrgDto dto)
        {
            var userId = GetUserId();
            var org = await _db.Organizations.Include(o => o.Members).FirstOrDefaultAsync(o => o.Id == id);
            if (org == null) return NotFound();

            var membership = org.Members.FirstOrDefault(m => m.UserId == userId);
            if (membership == null || (membership.OrgRole != OrgRole.OWNER && membership.OrgRole != OrgRole.ADMIN))
                return StatusCode(403, new ResponseDTO { IsSuccess = false, Message = "Chỉ Owner/Admin mới được sửa." });

            if (!string.IsNullOrWhiteSpace(dto.Name)) org.Name = dto.Name;
            if (dto.Description != null) org.Description = dto.Description;
            await _db.SaveChangesAsync();
            return Ok(new ResponseDTO { Result = new { org.Id, org.Name, org.Description } });
        }

        // ── DELETE /api/organizations/{id} ────────────────────────────────
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            var org = await _db.Organizations.Include(o => o.Members).FirstOrDefaultAsync(o => o.Id == id);
            if (org == null) return NotFound();

            var membership = org.Members.FirstOrDefault(m => m.UserId == userId);
            if (membership?.OrgRole != OrgRole.OWNER)
                return StatusCode(403, new ResponseDTO { IsSuccess = false, Message = "Chỉ Owner mới được xoá tổ chức." });

            _db.Organizations.Remove(org);
            await _db.SaveChangesAsync();
            return Ok(new ResponseDTO { Message = "Đã xoá tổ chức." });
        }

        // ── PUT /api/organizations/{id}/members/{memberId}/role ───────────
        [HttpPut("{id}/members/{memberId}/role")]
        public async Task<IActionResult> UpdateMemberRole(int id, int memberId, [FromBody] UpdateOrgMemberRoleDto dto)
        {
            var userId = GetUserId();
            var myRole = await _db.OrganizationMembers
                .Where(m => m.OrganizationId == id && m.UserId == userId)
                .Select(m => m.OrgRole).FirstOrDefaultAsync();

            if (myRole != OrgRole.OWNER)
                return StatusCode(403, new ResponseDTO { IsSuccess = false, Message = "Chỉ Owner mới được đổi role." });

            var target = await _db.OrganizationMembers
                .FirstOrDefaultAsync(m => m.OrganizationId == id && m.UserId == memberId);
            if (target == null) return NotFound();
            if (target.OrgRole == OrgRole.OWNER)
                return BadRequest(new ResponseDTO { IsSuccess = false, Message = "Không thể đổi role của Owner." });

            target.OrgRole = dto.OrgRole;
            await _db.SaveChangesAsync();
            return Ok(new ResponseDTO { Message = "Đã đổi quyền." });
        }
    }

    public class CreateOrgDto
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
    }

    public class UpdateOrgDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    public class InviteOrgMemberDto
    {
        public string Email { get; set; } = "";
        public string? Role { get; set; }
    }

    public class UpdateOrgMemberRoleDto
    {
        public string OrgRole { get; set; } = "MEMBER";
    }
}
