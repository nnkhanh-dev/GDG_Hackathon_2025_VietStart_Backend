using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VietStart_API.Enums;

namespace VietStart_API.Entities.Domains
{
    public class StartUpMedia
    {
        [Key]
        public int Id { get; set; }
        public string Path { get; set; }
        public MediaType Type { get; set; } 
        public int StartUpId { get; set; }
        [ForeignKey("StartUpId")]
        public StartUp StartUp { get; set; }
    }
}
