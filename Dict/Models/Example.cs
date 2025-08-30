using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class Example
    {
        public int Id { get; set; }

        public int? SenseId { get; set; }
        public virtual Sense Sense { get; set; }

        public string ContentJp { get; set; }
        public string ContentTranslated { get; set; }

        public string Transcription { get; set; }

        public string SourceRef { get; set; }
    }

}
