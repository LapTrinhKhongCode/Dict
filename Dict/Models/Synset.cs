using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class Synset
    {
        public int Id { get; set; }

        public int? SenseId { get; set; }
        public virtual Sense Sense { get; set; }

        public string BaseForm { get; set; }

        public string Synonyms { get; set; }

        public string Pos { get; set; }
    }

}
