using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VietStart_API.Enums;

namespace VietStart_API.Entities.Domains
{
    public class TeamStartUp
    {
        [Key]
        public int Id { get; set; }
        public int StartUpId { get; set; }
        public string UserId { get; set; }    
        public TeamStartUpStatus Status { get; set; } = TeamStartUpStatus.Pending; // Trạng thái: Pending, Dealing, Success, Rejected

        [ForeignKey(nameof(UserId))]
        public AppUser User { get; set; }
        [ForeignKey(nameof(StartUpId))]
        public StartUp StartUp { get; set; }
    }
}
