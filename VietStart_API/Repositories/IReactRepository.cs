using VietStart_API.Entities.Domains;

namespace VietStart_API.Repositories
{
    public interface IReactRepository : IGenericRepository<React>
    {
        Task<IEnumerable<React>> GetReactsByStartupAsync(int startupId);
        Task<IEnumerable<React>> GetReactsByCommentAsync(int commentId);
        Task<React> GetUserReactOnStartupAsync(string userId, int startupId);
        Task<React> GetUserReactOnCommentAsync(string userId, int commentId);
    }
}
