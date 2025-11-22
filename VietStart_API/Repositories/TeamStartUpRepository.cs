using Microsoft.EntityFrameworkCore;
using VietStart_API.Data;
using VietStart_API.Entities.Domains;

namespace VietStart_API.Repositories
{
    public class TeamStartUpRepository : GenericRepository<TeamStartUp>, ITeamStartUpRepository
    {
        public TeamStartUpRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<TeamStartUp> GetTeamStartUpWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(t => t.User)
                .Include(t => t.StartUp)
                    .ThenInclude(s => s.Category)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<IEnumerable<TeamStartUp>> GetTeamStartUpsByStartUpIdAsync(int startUpId)
        {
            return await _dbSet
                .Include(t => t.User)
                .Where(t => t.StartUpId == startUpId)
                .ToListAsync();
        }

        public async Task<IEnumerable<TeamStartUp>> GetTeamStartUpsByUserIdAsync(string userId)
        {
            return await _dbSet
                .Include(t => t.StartUp)
                    .ThenInclude(s => s.Category)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Id)
                .ToListAsync();
        }

        public async Task<IEnumerable<TeamStartUp>> GetTeamStartUpsByStatusAsync(string status)
        {
            return await _dbSet
                .Include(t => t.User)
                .Include(t => t.StartUp)
                .Where(t => t.Status == status)
                .ToListAsync();
        }

        public async Task<TeamStartUp?> GetPendingRequestAsync(int startUpId, string userId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(t => t.StartUpId == startUpId && 
                                         t.UserId == userId && 
                                         t.Status == "Pending");
        }

        public async Task<IEnumerable<TeamStartUp>> GetPendingRequestsByStartUpIdAsync(int startUpId)
        {
            return await _dbSet
                .Include(t => t.User)
                .Where(t => t.StartUpId == startUpId && t.Status == "Pending")
                .ToListAsync();
        }
    }
}
