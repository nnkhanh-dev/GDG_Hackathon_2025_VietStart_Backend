using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VietStart_API.Entities.Domains
{
    public class UserRoleEmbading
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey(nameof(UserId))]
        public string UserId { get; set; }
        public AppUser User { get; set; }
        public int RoleEmbadingId { get; set; }
        [ForeignKey(nameof(RoleEmbadingId))]
        public RoleEmbading RoleEmbading { get; set; }
    }
}
