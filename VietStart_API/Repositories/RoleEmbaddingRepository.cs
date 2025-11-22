using VietStart_API.Data;
using VietStart_API.Entities.Domains;

namespace VietStart_API.Repositories
{
    public class RoleEmbaddingRepository : GenericRepository<RoleEmbadding>, IRoleEmbaddingRepository
    {
        public RoleEmbaddingRepository(AppDbContext context) : base(context)
        {
        }
    }
}
