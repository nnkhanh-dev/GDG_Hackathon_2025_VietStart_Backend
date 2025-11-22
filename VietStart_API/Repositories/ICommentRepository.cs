using VietStart_API.Entities.Domains;

namespace VietStart_API.Repositories
{
    public interface ICommentRepository : IGenericRepository<Comment>
    {
        Task<IEnumerable<Comment>> GetCommentsByStartupAsync(int startupId);
        Task<Comment> GetCommentWithRepliesAsync(int id);
    }
}
