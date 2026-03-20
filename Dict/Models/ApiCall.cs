using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dict.Models
{
    public class ApiCall
    {
        public int Id { get; set; }

        public string Endpoint { get; set; }

        public string RequestJson { get; set; }
        public int? ResponseStatus { get; set; }
        public int? ResponseTimeMs { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
