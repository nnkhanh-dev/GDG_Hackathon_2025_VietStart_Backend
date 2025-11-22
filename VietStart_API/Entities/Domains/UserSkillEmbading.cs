using System.ComponentModel.DataAnnotations.Schema;

namespace VietStart_API.Entities.Domains
{
    public class UserSkillEmbading
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public AppUser User { get; set; }
        public int SkillEmbadingId { get; set; }
        [ForeignKey(nameof(SkillEmbadingId))]
        public SkillEmbading SkillEmbading { get; set; }
    }
}
