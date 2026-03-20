using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class Gloss
    {
        public int Id { get; set; }

        public int? SenseId { get; set; }
        public virtual Sense Sense { get; set; }

        public int? LanguageId { get; set; }
        public virtual Language Language { get; set; }

        public string Text { get; set; }

        public string GType { get; set; }

        public string GGend { get; set; }

        public string Priority { get; set; }
    }
}
