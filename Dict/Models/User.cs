using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; }

        public string Email { get; set; }

        public string PasswordHash { get; set; }

        public bool IsActive { get; set; }

        public string Role { get; set; }

        public string AvatarUrl { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<Deck> Decks { get; set; }
        public virtual ICollection<CardState> CardStates { get; set; }
        public virtual ICollection<ReviewLog> ReviewLogs { get; set; }
        public virtual ICollection<MediaStore> MediaStore { get; set; }
        public virtual ICollection<OcrJob> OcrJobs { get; set; }
    }
}
