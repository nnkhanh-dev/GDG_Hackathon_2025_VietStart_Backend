using VietStart_API.Entities.Domains;
using VietStart_API.Entities.DTO;

namespace VietStart_API.Services
{
    public interface IEmbeddingService
    {
        Task<string> GetEmbeddingAsync(string text);
        Task<double> CalculateCosineSimilarityAsync(string embedding1, string embedding2);
        Task<List<UserSuggestionDto>> GetSuggestedUsersForStartupAsync(StartUp startup);
    }
}
