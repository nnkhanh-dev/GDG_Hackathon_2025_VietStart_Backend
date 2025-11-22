using VietStart_API.Entities.Domains;

namespace VietStart_API.Repositories
{
    public interface IAppUserRepository : IGenericRepository<AppUser>
    {
        Task<AppUser> GetUserWithDetailsAsync(string userId);
        Task<IEnumerable<AppUser>> SearchUsersAsync(string keyword);
    }
}
