using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    // Lớp này đại diện cho bảng dbo.SynsetEntries
    // Mỗi object là một "nhóm từ đồng nghĩa"
    public class SynsetEntry
    {
        public int Id { get; set; }
        public string BaseForm { get; set; }
        public string Pos { get; set; }
        public string? DefinitionId { get; set; }

        public int SenseId { get; set; }
        [ForeignKey("SenseId")]
        public virtual Sense Sense { get; set; }

        // Navigation property đến các từ đồng nghĩa con
        public virtual ICollection<SynonymItem> SynonymItems { get; set; } = new List<SynonymItem>();
    }
}