using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class Inflection
    {
        public int Id { get; set; }

        public int? LemmaId { get; set; }
        public virtual Lemma Lemma { get; set; }

        public string FormText { get; set; }

        public string FormType { get; set; }
    }
}
