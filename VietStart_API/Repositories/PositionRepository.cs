using Microsoft.EntityFrameworkCore;
using VietStart_API.Data;
using VietStart_API.Entities.Domains;
using VietStart_API.Repositories;

namespace VietStart_API.Repositories
{
    public class PositionRepository : GenericRepository<Position>, IPositionRepository
    {
        public PositionRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Position> GetPositionByNameAsync(string name)
        {
            return await _dbSet
                .FirstOrDefaultAsync(p => p.Name.ToLower() == name.ToLower());
        }

        public async Task<IEnumerable<Position>> SearchPositionsAsync(string keyword)
        {
            return await _dbSet
                .Where(p => p.Name.Contains(keyword))
                .OrderBy(p => p.Name)
                .ToListAsync();
        }
    }
}
