using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class StatsWordFreq
    {
        public int Id { get; set; }

        public int? WordId { get; set; }
        public virtual Word Word { get; set; }

        public int? Occurrences { get; set; }
        public DateTime? LastSeenAt { get; set; }
    }

}
