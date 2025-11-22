using VietStart_API.Entities.Domains;

namespace VietStart_API.Repositories
{
    public interface ICategoryRepository : IGenericRepository<Category>
    {
        Task<IEnumerable<Category>> GetCategoriesWithStartupsCountAsync();
    }
}
