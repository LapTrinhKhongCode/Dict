using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class ImportJob
    {
        public int Id { get; set; }

        public string JobType { get; set; }

        public string Status { get; set; }

        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }

        public string Meta { get; set; } // json
    }

}
