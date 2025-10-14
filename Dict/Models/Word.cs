using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class Word
    {
        public int Id { get; set; }

        public int? EntryId { get; set; }
        public virtual Entry Entry { get; set; }

        public string WordText { get; set; }

        public string Phonetic { get; set; }

        public string Romaji { get; set; }

        public string ShortMean { get; set; }

        public int? Weight { get; set; }
        public int? MobileId { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<WordKanji> WordKanji { get; set; }
        public virtual ICollection<WordToEntry> WordToEntries { get; set; }
        public virtual ICollection<Media> Media { get; set; }
        public virtual ICollection<WordRelation> Relations { get; set; }
        public virtual ICollection<WordTag> WordTags { get; set; }
        [InverseProperty("Word")]
        public virtual ICollection<Translation> Translations { get; set; }
        [InverseProperty("RelatedWord")]
        public virtual ICollection<WordRelation> AppearingInRelations { get; set; } = new List<WordRelation>();
        public virtual ICollection<StatsWordFreq> WordFreqStats { get; set; }
        public virtual ICollection<OcrResult> OcrResults { get; set; }
        public virtual ICollection<Card> Cards { get; set; }
        public virtual ICollection<Lemma> Lemmas { get; set; }
        public virtual ICollection<SynonymsCache> SynonymsCaches { get; set; }
    }

}
