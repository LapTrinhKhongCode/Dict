using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Dict.Enums;

namespace Dict.Models
{
    public class WorkspaceInvitation
    {
        [Key]
        public int Id { get; set; }

        public int WorkspaceId { get; set; }

        public int InviteeId { get; set; } // Người được mời
        public int InviterId { get; set; } // Admin thực hiện mời

        [Required]
        public string ExpectedRole { get; set; } // Dùng WorkspaceRole.ADMIN hoặc WorkspaceRole.MEMBER

        public InvitationStatus Status { get; set; } = InvitationStatus.PENDING;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // --- Navigation Properties ---
        [ForeignKey(nameof(WorkspaceId))]
        public virtual Workspace Workspace { get; set; }

        [ForeignKey(nameof(InviteeId))]
        public virtual ApplicationUser Invitee { get; set; }

        [ForeignKey(nameof(InviterId))]
        public virtual ApplicationUser Inviter { get; set; }
    }
}