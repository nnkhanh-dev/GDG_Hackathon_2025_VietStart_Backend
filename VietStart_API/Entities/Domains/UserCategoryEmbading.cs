using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VietStart_API.Entities.Domains
{
    public class UserCategoryEmbading
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public AppUser User { get; set; }
        public int CategoryEmBadingId { get; set; }

        [ForeignKey(nameof(CategoryEmBadingId))]
        public CategoryEmbading CategoryEmBading { get; set; }
    }
}
