using VietStart_API.Data;
using VietStart_API.Entities.Domains;

namespace VietStart_API.Repositories
{
    public class SkillEmbaddingRepository : GenericRepository<SkillEmbadding>, ISkillEmbaddingRepository
    {
        public SkillEmbaddingRepository(AppDbContext context) : base(context)
        {
        }
    }
}
