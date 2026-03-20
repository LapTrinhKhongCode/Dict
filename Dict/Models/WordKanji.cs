using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class WordKanji
    {
        // composite PK configured in OnModelCreating
        public int WordId { get; set; }
        public int KanjiId { get; set; }

        public virtual Word Word { get; set; }

        public virtual Kanji Kanji { get; set; }
    }
}
