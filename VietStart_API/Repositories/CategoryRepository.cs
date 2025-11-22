using Microsoft.EntityFrameworkCore;
using VietStart_API.Data;
using VietStart_API.Entities.Domains;

namespace VietStart_API.Repositories
{
    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Category>> GetCategoriesWithStartupsCountAsync()
        {
            return await _dbSet
                .Where(c => c.DeletedAt == null)
                .Include(c => c.StartUps.Where(s => s.DeletedAt == null))
                .ToListAsync();
        }
    }
}
