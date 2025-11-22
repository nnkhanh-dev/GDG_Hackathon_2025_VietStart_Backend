using VietStart_API.Entities.Domains;

namespace VietStart_API.Repositories
{
    public interface ITeamStartUpRepository : IGenericRepository<TeamStartUp>
    {
        Task<TeamStartUp> GetTeamStartUpWithDetailsAsync(int id);
        Task<IEnumerable<TeamStartUp>> GetTeamStartUpsByStartUpIdAsync(int startUpId);
        Task<IEnumerable<TeamStartUp>> GetTeamStartUpsByUserIdAsync(string userId);
        Task<IEnumerable<TeamStartUp>> GetTeamStartUpsByStatusAsync(string status);
        Task<TeamStartUp?> GetPendingRequestAsync(int startUpId, string userId);
        Task<IEnumerable<TeamStartUp>> GetPendingRequestsByStartUpIdAsync(int startUpId);
    }
}
