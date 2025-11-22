using VietStart_API.Entities.Domains;

namespace VietStart_API.Repositories
{
    public interface IStartUpMediaRepository : IGenericRepository<StartUpMedia>
    {
        Task<IEnumerable<StartUpMedia>> GetMediasByStartupAsync(int startupId);
    }
}
