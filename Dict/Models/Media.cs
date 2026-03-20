using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class Media
    {
        public int Id { get; set; }

        public int? EntryId { get; set; }
        public virtual Entry Entry { get; set; }

        public int? WordId { get; set; }
        public virtual Word Word { get; set; }

        public string Url { get; set; }

        public string Caption { get; set; }

        public string MediaType { get; set; }

        public string Source { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
