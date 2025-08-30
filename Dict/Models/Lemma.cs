using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class Lemma
    {
        public int Id { get; set; }

        public int? WordId { get; set; }
        public virtual Word Word { get; set; }

        public string LemmaText { get; set; }

        public string Pos { get; set; }

        public virtual ICollection<Inflection> Inflections { get; set; }
    }
}
