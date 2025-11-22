using Microsoft.EntityFrameworkCore;
using VietStart_API.Data;
using VietStart_API.Entities.Domains;

namespace VietStart_API.Repositories
{
    public class ReactRepository : GenericRepository<React>, IReactRepository
    {
        public ReactRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<React>> GetReactsByStartupAsync(int startupId)
        {
            return await _dbSet
                .Where(r => r.StartUpId == startupId)
                .Include(r => r.User)
                .ToListAsync();
        }

        public async Task<IEnumerable<React>> GetReactsByCommentAsync(int commentId)
        {
            return await _dbSet
                .Where(r => r.CommentId == commentId)
                .Include(r => r.User)
                .ToListAsync();
        }

        public async Task<React> GetUserReactOnStartupAsync(string userId, int startupId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(r => r.UserId == userId && r.StartUpId == startupId);
        }

        public async Task<React> GetUserReactOnCommentAsync(string userId, int commentId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(r => r.UserId == userId && r.CommentId == commentId);
        }
    }
}
