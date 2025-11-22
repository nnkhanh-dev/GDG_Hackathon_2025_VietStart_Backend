using Microsoft.EntityFrameworkCore;
using VietStart_API.Data;
using VietStart_API.Entities.Domains;

namespace VietStart_API.Repositories
{
    public class ShareRepository : GenericRepository<Share>, IShareRepository
    {
        public ShareRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Share>> GetSharesByStartupAsync(int startupId)
        {
            return await _dbSet
                .Where(s => s.StartUpId == startupId && s.DeletedAt == null)
                .Include(s => s.User)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Share>> GetSharesByUserAsync(string userId)
        {
            return await _dbSet
                .Where(s => s.UserId == userId && s.DeletedAt == null)
                .Include(s => s.User)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<Share> GetShareAsync(string userId, int startupId)
        {
            return await _dbSet
                .Where(s => s.UserId == userId && s.StartUpId == startupId && s.DeletedAt == null)
                .Include(s => s.User)
                .FirstOrDefaultAsync();
        }
    }
}
