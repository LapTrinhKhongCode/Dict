using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class KanjiElement
    {
        public int Id { get; set; }

        public int? EntryId { get; set; }
        public virtual Entry Entry { get; set; }

        public string Keb { get; set; }

        public string KeInf { get; set; }

        public string KePri { get; set; }
    }
}
