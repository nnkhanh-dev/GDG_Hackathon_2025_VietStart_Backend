using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using VietStart_API.Data;
using VietStart_API.Entities.Domains;
using VietStart_API.Entities.DTO;

namespace VietStart_API.Services
{
    public class EmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private readonly string _geminiEmbeddingUrl;

        // Trọng số cho từng tiêu chí (tổng = 1.0)
        private const double WEIGHT_SKILLS = 0.40;      // TeamEmbedding vs SkillsEmbadding
        private const double WEIGHT_ROLES = 0.35;       // TeamEmbedding vs RolesEmbadding
        private const double WEIGHT_CATEGORIES = 0.25;  // Category (on-the-fly) vs CategoriesEmbadding

        public EmbeddingService(HttpClient httpClient, IConfiguration configuration, AppDbContext context)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _context = context;

            string apiKey = _configuration["Gemini:Key"];
            // Sử dụng model text-embedding-004 với method embedContent
            _geminiEmbeddingUrl = $"https://generativelanguage.googleapis.com/v1beta/models/text-embedding-004:embedContent?key={apiKey}";
        }

        /// <summary>
        /// Lấy embedding vector từ Gemini API cho một đoạn text
        /// </summary>
        public async Task<string> GetEmbeddingAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "[]";

            try
            {
                // Request body đúng format cho embedContent API
                var requestBody = new
                {
                    content = new
                    {
                        parts = new[] { new { text = text } }
                    }
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(_geminiEmbeddingUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Gemini API Error: {response.StatusCode} - {errorContent}");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonResponse);

                if (doc.RootElement.TryGetProperty("embedding", out var embeddingObj) &&
                    embeddingObj.TryGetProperty("values", out var values))
                {
                    var embedding = values.EnumerateArray()
                        .Select(v => v.GetDouble())
                        .ToArray();

                    return JsonSerializer.Serialize(embedding);
                }

                return "[]";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting embedding: {ex.Message}");
                return "[]";
            }
        }

        /// <summary>
        /// Tính cosine similarity giữa 2 embedding vectors
        /// </summary>
        public async Task<double> CalculateCosineSimilarityAsync(string embedding1Json, string embedding2Json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(embedding1Json) || string.IsNullOrWhiteSpace(embedding2Json))
                    return 0.0;

                var vector1 = JsonSerializer.Deserialize<double[]>(embedding1Json);
                var vector2 = JsonSerializer.Deserialize<double[]>(embedding2Json);

                if (vector1 == null || vector2 == null || vector1.Length != vector2.Length || vector1.Length == 0)
                    return 0.0;

                return await Task.Run(() =>
                {
                    double dotProduct = 0.0;
                    double magnitude1 = 0.0;
                    double magnitude2 = 0.0;

                    for (int i = 0; i < vector1.Length; i++)
                    {
                        dotProduct += vector1[i] * vector2[i];
                        magnitude1 += vector1[i] * vector1[i];
                        magnitude2 += vector2[i] * vector2[i];
                    }

                    magnitude1 = Math.Sqrt(magnitude1);
                    magnitude2 = Math.Sqrt(magnitude2);

                    if (magnitude1 == 0 || magnitude2 == 0)
                        return 0.0;

                    return dotProduct / (magnitude1 * magnitude2);
                });
            }
            catch
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Tìm những người dùng phù hợp nhất cho startup dựa trên weighted cosine similarity
        /// </summary>
        public async Task<List<UserSuggestionDto>> GetSuggestedUsersForStartupAsync(StartUp startup)
        {
            try
            {
                // Lấy tất cả users có embedding (đã được tính trước)
                var users = await _context.Users
                    .Where(u => u.DeletedAt == null && 
                                u.Id != startup.UserId && 
                                (!string.IsNullOrEmpty(u.SkillsEmbadding) || 
                                 !string.IsNullOrEmpty(u.RolesEmbadding) || 
                                 !string.IsNullOrEmpty(u.CategoriesEmbadding)))
                    .ToListAsync();

                if (!users.Any())
                {
                    Console.WriteLine("No users found with embeddings");
                    return new List<UserSuggestionDto>();
                }

                Console.WriteLine($"Found {users.Count} users to compare");

                var suggestions = new List<UserSuggestionDto>();

                foreach (var user in users)
                {
                    double totalScore = 0.0;
                    int comparisonCount = 0;
                    var details = new Dictionary<string, double>();

                    // 1. So sánh TeamEmbedding của Startup với SkillsEmbadding của User (40%)
                    if (!string.IsNullOrEmpty(startup.TeamEmbedding) && 
                        !string.IsNullOrEmpty(user.SkillsEmbadding))
                    {
                        var skillsSimilarity = await CalculateCosineSimilarityAsync(
                            startup.TeamEmbedding, 
                            user.SkillsEmbadding);
                        var skillsScore = skillsSimilarity * WEIGHT_SKILLS;
                        totalScore += skillsScore;
                        comparisonCount++;
                        details["TeamVsSkills"] = Math.Round(skillsSimilarity * 100, 2);
                        Console.WriteLine($"User {user.FullName} - Team vs Skills similarity: {skillsSimilarity:F4}");
                    }

                    // 2. So sánh TeamEmbedding của Startup với RolesEmbadding của User (35%)
                    if (!string.IsNullOrEmpty(startup.TeamEmbedding) && 
                        !string.IsNullOrEmpty(user.RolesEmbadding))
                    {
                        var rolesSimilarity = await CalculateCosineSimilarityAsync(
                            startup.TeamEmbedding, 
                            user.RolesEmbadding);
                        var rolesScore = rolesSimilarity * WEIGHT_ROLES;
                        totalScore += rolesScore;
                        comparisonCount++;
                        details["TeamVsRoles"] = Math.Round(rolesSimilarity * 100, 2);
                        Console.WriteLine($"User {user.FullName} - Team vs Roles similarity: {rolesSimilarity:F4}");
                    }

                    // 3. So sánh Category của Startup (tính on-the-fly) với CategoriesEmbadding của User (25%)
                    if (startup.CategoryId > 0 && !string.IsNullOrEmpty(user.CategoriesEmbadding))
                    {
                        var startupCategory = await _context.Categories
                            .FirstOrDefaultAsync(c => c.Id == startup.CategoryId);
                        
                        if (startupCategory != null)
                        {
                            var categoryText = startupCategory.Name;
                            if (!string.IsNullOrEmpty(startupCategory.Description))
                            {
                                categoryText += " " + startupCategory.Description;
                            }
                            var startupCategoryEmbedding = await GetEmbeddingAsync(categoryText);
                            
                            var categorySimilarity = await CalculateCosineSimilarityAsync(
                                startupCategoryEmbedding, 
                                user.CategoriesEmbadding);
                            var categoryScore = categorySimilarity * WEIGHT_CATEGORIES;
                            totalScore += categoryScore;
                            comparisonCount++;
                            details["CategoryVsCategories"] = Math.Round(categorySimilarity * 100, 2);
                            Console.WriteLine($"User {user.FullName} - Category similarity: {categorySimilarity:F4}");
                        }
                    }

                    Console.WriteLine($"User {user.FullName} - Total score: {totalScore:F4} ({comparisonCount} comparisons)");

                    // Chỉ thêm vào danh sách nếu có ít nhất 1 so sánh và điểm > 0
                    if (comparisonCount > 0 && totalScore > 0)
                    {
                        suggestions.Add(new UserSuggestionDto
                        {
                            UserId = user.Id,
                            FullName = user.FullName,
                            Email = user.Email,
                            Avatar = user.Avatar,
                            Bio = user.Bio,
                            Location = user.Location,
                            Skills = user.Skills,
                            RolesInStartup = user.RolesInStartup,
                            MatchScore = Math.Round(totalScore * 100, 2), // Chuyển sang %
                            MatchDetails = details
                        });
                    }
                }

                Console.WriteLine($"Found {suggestions.Count} suggestions before filtering");

                // Sắp xếp theo điểm từ cao xuống thấp và lấy top 20
                return suggestions
                    .OrderByDescending(s => s.MatchScore)
                    .Take(20)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting suggested users: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return new List<UserSuggestionDto>();
            }
        }

        /// <summary>
        /// Tìm những người dùng phù hợp nhất cho startup và nhóm theo Skills, Roles, Categories
        /// </summary>
        public async Task<GroupedSuggestionsResponseDto> GetGroupedSuggestedUsersForStartupAsync(StartUp startup)
        {
            try
            {
                // Lấy tất cả users có embedding
                var users = await _context.Users
                    .Where(u => u.DeletedAt == null && 
                                u.Id != startup.UserId && 
                                (!string.IsNullOrEmpty(u.SkillsEmbadding) || 
                                 !string.IsNullOrEmpty(u.RolesEmbadding) || 
                                 !string.IsNullOrEmpty(u.CategoriesEmbadding)))
                    .ToListAsync();

                if (!users.Any())
                {
                    Console.WriteLine("No users found with embeddings");
                    return new GroupedSuggestionsResponseDto();
                }

                Console.WriteLine($"Found {users.Count} users to compare for grouped suggestions");

                var groupedUsers = new List<GroupedUserSuggestionDto>();

                // Tính embedding cho category của startup nếu cần
                string startupCategoryEmbedding = null;
                if (startup.CategoryId > 0)
                {
                    var startupCategory = await _context.Categories
                        .FirstOrDefaultAsync(c => c.Id == startup.CategoryId);
                    
                    if (startupCategory != null)
                    {
                        var categoryText = startupCategory.Name;
                        if (!string.IsNullOrEmpty(startupCategory.Description))
                        {
                            categoryText += " " + startupCategory.Description;
                        }
                        startupCategoryEmbedding = await GetEmbeddingAsync(categoryText);
                    }
                }

                foreach (var user in users)
                {
                    double skillScore = 0.0;
                    double roleScore = 0.0;
                    double categoryScore = 0.0;

                    // 1. So sánh TeamEmbedding với SkillsEmbadding
                    if (!string.IsNullOrEmpty(startup.TeamEmbedding) && 
                        !string.IsNullOrEmpty(user.SkillsEmbadding))
                    {
                        skillScore = await CalculateCosineSimilarityAsync(
                            startup.TeamEmbedding, 
                            user.SkillsEmbadding);
                        Console.WriteLine($"User {user.FullName} - Skill similarity: {skillScore:F4}");
                    }

                    // 2. So sánh TeamEmbedding với RolesEmbadding
                    if (!string.IsNullOrEmpty(startup.TeamEmbedding) && 
                        !string.IsNullOrEmpty(user.RolesEmbadding))
                    {
                        roleScore = await CalculateCosineSimilarityAsync(
                            startup.TeamEmbedding, 
                            user.RolesEmbadding);
                        Console.WriteLine($"User {user.FullName} - Role similarity: {roleScore:F4}");
                    }

                    // 3. So sánh Category với CategoriesEmbadding
                    if (!string.IsNullOrEmpty(startupCategoryEmbedding) && 
                        !string.IsNullOrEmpty(user.CategoriesEmbadding))
                    {
                        categoryScore = await CalculateCosineSimilarityAsync(
                            startupCategoryEmbedding, 
                            user.CategoriesEmbadding);
                        Console.WriteLine($"User {user.FullName} - Category similarity: {categoryScore:F4}");
                    }

                    // Tính overall score theo trọng số
                    double overallScore = (skillScore * WEIGHT_SKILLS) + 
                                         (roleScore * WEIGHT_ROLES) + 
                                         (categoryScore * WEIGHT_CATEGORIES);

                    Console.WriteLine($"User {user.FullName} - Overall score: {overallScore:F4}");

                    // Thêm vào danh sách nếu có ít nhất 1 điểm > 0
                    if (skillScore > 0 || roleScore > 0 || categoryScore > 0)
                    {
                        groupedUsers.Add(new GroupedUserSuggestionDto
                        {
                            UserId = user.Id,
                            FullName = user.FullName,
                            Email = user.Email,
                            Avatar = user.Avatar,
                            Bio = user.Bio,
                            Location = user.Location,
                            Skills = user.Skills,
                            RolesInStartup = user.RolesInStartup,
                            SkillMatchScore = Math.Round(skillScore * 100, 2),
                            RoleMatchScore = Math.Round(roleScore * 100, 2),
                            CategoryMatchScore = Math.Round(categoryScore * 100, 2),
                            OverallScore = Math.Round(overallScore * 100, 2)
                        });
                    }
                }

                Console.WriteLine($"Found {groupedUsers.Count} users with scores");

                // Nhóm và sắp xếp theo từng tiêu chí
                var response = new GroupedSuggestionsResponseDto
                {
                    // Top 10 theo Skills
                    BySkills = groupedUsers
                        .Where(u => u.SkillMatchScore > 0)
                        .OrderByDescending(u => u.SkillMatchScore)
                        .Take(50)
                        .ToList(),

                    // Top 10 theo Roles
                    ByRoles = groupedUsers
                        .Where(u => u.RoleMatchScore > 0)
                        .OrderByDescending(u => u.RoleMatchScore)
                        .Take(50)
                        .ToList(),

                    // Top 10 theo Category
                    ByCategory = groupedUsers
                        .Where(u => u.CategoryMatchScore > 0)
                        .OrderByDescending(u => u.CategoryMatchScore)
                        .Take(50)
                        .ToList(),

                    // Top 20 theo Overall (tổng hợp)
                    Overall = groupedUsers
                        .OrderByDescending(u => u.OverallScore)
                        .Take(50)
                        .ToList()
                };

                Console.WriteLine($"Grouped suggestions - Skills: {response.BySkills.Count}, Roles: {response.ByRoles.Count}, Category: {response.ByCategory.Count}, Overall: {response.Overall.Count}");

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting grouped suggested users: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return new GroupedSuggestionsResponseDto();
            }
        }
    }
}
