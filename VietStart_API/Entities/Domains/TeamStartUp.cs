using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VietStart_API.Entities.Domains
{
    public class TeamStartUp
    {
        [Key]
        public int Id { get; set; }
        public int StartUpId { get; set; }
        public string UserId { get; set; }    
        public string Status { get; set; } // Trạng thái tham gia (đang chờ, đã chấp nhận, từ chối)

        [ForeignKey(nameof(UserId))]
        public AppUser User { get; set; }
        [ForeignKey(nameof(StartUpId))]
        public StartUp StartUp { get; set; }
    }
}
