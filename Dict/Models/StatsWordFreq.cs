using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class StatsWordFreq
    {
        public int Id { get; set; }

        public int? EntryId { get; set; }

        public int? Occurrences { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public virtual Entry Entry { get; set; }
    }

}
