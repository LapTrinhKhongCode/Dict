using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class ReadingElement
    {
        public int Id { get; set; }

        public int? EntryId { get; set; }
        public virtual Entry Entry { get; set; }

        public string Reb { get; set; }

        public string ReNoKanji { get; set; }

        public string RePri { get; set; }
    }
}
