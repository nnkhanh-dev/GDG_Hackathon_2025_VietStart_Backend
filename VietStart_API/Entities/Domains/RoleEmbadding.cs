using System.ComponentModel.DataAnnotations;

namespace VietStart_API.Entities.Domains
{
    public class RoleEmbadding
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
