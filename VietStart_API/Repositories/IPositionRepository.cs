using VietStart_API.Entities.Domains;

namespace VietStart_API.Repositories
{
    public interface IPositionRepository : IGenericRepository<Position>
    {
        Task<Position> GetPositionByNameAsync(string name);
        Task<IEnumerable<Position>> SearchPositionsAsync(string keyword);
    }
}
