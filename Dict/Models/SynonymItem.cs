using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    // Lớp này đại diện cho bảng dbo.SynonymItems
    // Mỗi object là một từ đồng nghĩa cụ thể
    public class SynonymItem
    {
        public int Id { get; set; }
        public string Word { get; set; }

        public int SynsetEntryId { get; set; }
        [ForeignKey("SynsetEntryId")]
        public virtual SynsetEntry SynsetEntry { get; set; }
    }
}