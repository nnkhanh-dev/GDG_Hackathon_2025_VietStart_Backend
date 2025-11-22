using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VietStart_API.Entities.Domains;
using VietStart_API.Entities.DTO;
using VietStart_API.Repositories;
using VietStart_API.Services;

namespace VietStart.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StartupsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IEmbeddingService _embeddingService;

        public StartupsController(IUnitOfWork unitOfWork, IMapper mapper, IEmbeddingService embeddingService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _embeddingService = embeddingService;
        }

        // GET: api/startups
        [HttpGet]
        [Authorize(Roles = "Admin,Client")]
        public async Task<ActionResult<IEnumerable<StartUpDto>>> GetStartups([FromQuery] int? categoryId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var (startups, total) = await _unitOfWork.StartUps.GetPaginatedAsync(
                page,
                pageSize,
                s => s.DeletedAt == null && (!categoryId.HasValue || s.CategoryId == categoryId.Value),
                q => q.OrderByDescending(s => s.CreatedAt));

            var startupDtos = startups.Select(s => new StartUpDto
            {
                Id = s.Id,
                Team = s.Team,
                Idea = s.Idea,
                Prototype = s.Prototype,
                Plan = s.Plan,
                Relationship = s.Relationship,
                Privacy = s.Privacy,
                Point = s.Point,
                IdeaPoint = s.IdeaPoint,
                TeamPoint = s.TeamPoint,
                PrototypePoint = s.PrototypePoint,
                PlanPoint = s.PlanPoint,
                RelationshipPoint = s.RelationshipPoint,
                UserId = s.UserId,
                UserFullName = s.AppUser.FullName,
                CategoryId = s.CategoryId,
                CategoryName = s.Category.Name,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                CommentCount = s.Comments?.Count ?? 0,
                ShareCount = s.Shares?.Count ?? 0,
                ReactCount = s.Reacts?.Count ?? 0
            }).ToList();

            return Ok(new { Data = startupDtos, Total = total });
        }

        // GET: api/startups/{id}/details
        [HttpGet("{id}/details")]
        [Authorize(Roles = "Admin,Client")]
        public async Task<ActionResult<StartUpDetailDto>> GetStartupDetails(int id)
        {
            var startup = await _unitOfWork.StartUps.GetStartUpWithDetailsAsync(id);

            if (startup == null)
                return NotFound(new { Message = "Startup không tồn tại" });

            var startupDetailDto = _mapper.Map<StartUpDetailDto>(startup);

            return Ok(startupDetailDto);
        }

        // GET: api/startups/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Client")]
        public async Task<ActionResult<StartUpDto>> GetStartup(int id)
        {
            var startup = await _unitOfWork.StartUps.FirstOrDefaultAsync(s => s.Id == id && s.DeletedAt == null);

            if (startup == null)
                return NotFound(new { Message = "Startup không tồn tại" });

            var category = await _unitOfWork.Categories.FirstOrDefaultAsync(c => c.Id == startup.CategoryId && c.DeletedAt == null);
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == startup.UserId);

            var startupDto = new StartUpDto
            {
                Id = startup.Id,
                Team = startup.Team,
                Idea = startup.Idea,
                Prototype = startup.Prototype,
                Plan = startup.Plan,
                Relationship = startup.Relationship,
                Privacy = startup.Privacy,
                Point = startup.Point,
                IdeaPoint = startup.IdeaPoint,
                TeamPoint = startup.TeamPoint,
                PrototypePoint = startup.PrototypePoint,
                PlanPoint = startup.PlanPoint,
                RelationshipPoint = startup.RelationshipPoint,
                UserId = startup.UserId,
                UserFullName = user?.FullName,
                CategoryId = startup.CategoryId,
                CategoryName = category?.Name,
                CreatedAt = startup.CreatedAt,
                UpdatedAt = startup.UpdatedAt
            };

            return Ok(startupDto);
        }

        // POST: api/startups
        [Authorize(Roles = "Admin,Client")]
        [HttpPost]
        public async Task<ActionResult<StartUpDto>> CreateStartup([FromBody] CreateStartUpDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if(userId == null)
                return Unauthorized();

            var category = await _unitOfWork.Categories.FirstOrDefaultAsync(c => c.Id == createDto.CategoryId && c.DeletedAt == null);
            if (category == null)
                return BadRequest(new { Message = "Danh mục không tồn tại" });

            var startup = _mapper.Map<StartUp>(createDto);
            startup.UserId = userId;
            startup.CreatedAt = DateTime.UtcNow;
            startup.CreatedBy = userId;

            // Tính embedding đồng bộ cho startup
            try
            {
                // Embedding cho Team
                if (!string.IsNullOrEmpty(startup.Team))
                {
                    startup.TeamEmbedding = await _embeddingService.GetEmbeddingAsync(startup.Team);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating embeddings: {ex.Message}");
                // Tiếp tục tạo startup ngay cả khi embedding lỗi
            }

            await _unitOfWork.StartUps.AddAsync(startup);

            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            var startupDto = new StartUpDto
            {
                Id = startup.Id,
                Team = startup.Team,
                Idea = startup.Idea,
                Prototype = startup.Prototype,
                Plan = startup.Plan,
                Relationship = startup.Relationship,
                Privacy = startup.Privacy,
                Point = startup.Point,
                IdeaPoint = startup.IdeaPoint,
                TeamPoint = startup.TeamPoint,
                PrototypePoint = startup.PrototypePoint,
                PlanPoint = startup.PlanPoint,
                RelationshipPoint = startup.RelationshipPoint,
                UserId = startup.UserId,
                UserFullName = user?.FullName,
                CategoryId = startup.CategoryId,
                CategoryName = category.Name,
                CreatedAt = startup.CreatedAt,
                UpdatedAt = startup.UpdatedAt
            };

            return CreatedAtAction(nameof(GetStartup), new { id = startup.Id }, startupDto);
        }

        // POST: api/startups/multi
        //[Authorize(Roles = "Admin,Client")]
        [HttpPost("multi")]
        public async Task<ActionResult> CreateMultiStartup([FromBody] CreateMultiStartUpDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var results = new List<object>();

            foreach (var dto in request.Startups)
            {
                try
                {
                    // Check UserId FE gửi lên
                    if (string.IsNullOrWhiteSpace(dto.UserId))
                    {
                        results.Add(new
                        {
                            Success = false,
                            Error = "UserId is required",
                            Data = dto
                        });
                        continue;
                    }

                    var userId = dto.UserId;

                    // Check category
                    var category = await _unitOfWork.Categories
                        .FirstOrDefaultAsync(c => c.Id == dto.CategoryId && c.DeletedAt == null);

                    if (category == null)
                    {
                        results.Add(new
                        {
                            Success = false,
                            Error = "Danh mục không tồn tại",
                            Data = dto
                        });
                        continue;
                    }

                    // Map → entity
                    var startup = _mapper.Map<StartUp>(dto);

                    startup.UserId = userId;
                    startup.CreatedAt = DateTime.UtcNow;
                    startup.CreatedBy = userId;

                    // Embedding
                    try
                    {
                        if (!string.IsNullOrEmpty(startup.Team))
                        {
                            startup.TeamEmbedding = await _embeddingService.GetEmbeddingAsync(startup.Team);
                        }
                    }
                    catch { }

                    // Save
                    await _unitOfWork.StartUps.AddAsync(startup);

                    // Build result item
                    results.Add(new
                    {
                        Success = true,
                        Startup = new
                        {
                            startup.Id,
                            startup.Team,
                            startup.Idea,
                            startup.Prototype,
                            startup.Plan,
                            startup.Relationship,
                            startup.Privacy,
                            startup.UserId,
                            CategoryId = category.Id,
                            CategoryName = category.Name,
                            startup.CreatedAt
                        }
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new
                    {
                        Success = false,
                        Error = ex.Message,
                        Data = dto
                    });
                }
            }

            return Ok(results);
        }



        // POST: api/startups/{id}/update
        [Authorize(Roles = "Admin,Client")]
        [HttpPost("{id}/update")]
        public async Task<IActionResult> UpdateStartup(int id, [FromBody] UpdateStartUpDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var startup = await _unitOfWork.StartUps.FirstOrDefaultAsync(s => s.Id == id && s.DeletedAt == null);

            if (startup == null)
                return NotFound(new { Message = "Startup không tồn tại" });

            if (startup.UserId != userId)
                return Forbid();

            var category = await _unitOfWork.Categories.FirstOrDefaultAsync(c => c.Id == updateDto.CategoryId && c.DeletedAt == null);
            if (category == null)
                return BadRequest(new { Message = "Danh mục không tồn tại" });

            _mapper.Map(updateDto, startup);
            startup.UpdatedAt = DateTime.UtcNow;
            startup.UpdatedBy = userId;

            await _unitOfWork.StartUps.UpdateAsync(startup);

            return Ok(new { Message = "Cập nhật startup thành công" });
        }

        // POST: api/startups/{id}/delete
        [Authorize(Roles = "Admin,Client")]
        [HttpPost("{id}/delete")]
        public async Task<IActionResult> DeleteStartup(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var startup = await _unitOfWork.StartUps.FirstOrDefaultAsync(s => s.Id == id && s.DeletedAt == null);

            if (startup == null)
                return NotFound(new { Message = "Startup không tồn tại" });

            if (startup.UserId != userId)
                return Forbid();

            startup.DeletedAt = DateTime.UtcNow;
            startup.DeletedBy = userId;

            await _unitOfWork.StartUps.UpdateAsync(startup);

            return Ok(new { Message = "Xóa startup thành công" });
        }

        // GET: api/startups/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<StartUpDto>>> GetUserStartups(string userId)
        {
            var startups = await _unitOfWork.StartUps.GetUserStartupsAsync(userId);

            var startupDtos = startups.Select(s => new StartUpDto
            {
                Id = s.Id,
                Team = s.Team,
                Idea = s.Idea,
                Prototype = s.Prototype,
                Plan = s.Plan,
                Relationship = s.Relationship,
                Privacy = s.Privacy,
                Point = s.Point,
                IdeaPoint = s.IdeaPoint,
                TeamPoint = s.TeamPoint,
                PrototypePoint = s.PrototypePoint,
                PlanPoint = s.PlanPoint,
                RelationshipPoint = s.RelationshipPoint,
                UserId = s.UserId,
                UserFullName = s.AppUser.FullName,
                CategoryId = s.CategoryId,
                CategoryName = s.Category.Name,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            }).ToList();

            return Ok(startupDtos);
        }

        // GET: api/startups/{id}/suggest-users
        [HttpGet("{id}/suggest-users")]
        [Authorize(Roles = "Admin,Client")]
        public async Task<ActionResult<IEnumerable<UserSuggestionDto>>> SuggestUsersForStartup(int id)
        {
            var startup = await _unitOfWork.StartUps.GetStartUpWithCategoryAsync(id);

            if (startup == null)
                return NotFound(new { Message = "Startup không tồn tại" });

            Console.WriteLine($"=== Suggesting users for Startup ID: {id} ===");
            Console.WriteLine($"Startup Team: {startup.Team}");
            Console.WriteLine($"Startup TeamEmbedding exists: {!string.IsNullOrEmpty(startup.TeamEmbedding)}");

            // Nếu chưa có embedding, tính ngay
            bool embeddingUpdated = false;
            if (string.IsNullOrEmpty(startup.TeamEmbedding))
            {
                try
                {
                    if (string.IsNullOrEmpty(startup.TeamEmbedding) && !string.IsNullOrEmpty(startup.Team))
                    {
                        Console.WriteLine("Calculating TeamEmbedding...");
                        startup.TeamEmbedding = await _embeddingService.GetEmbeddingAsync(startup.Team);
                        embeddingUpdated = true;
                        Console.WriteLine($"TeamEmbedding calculated: {startup.TeamEmbedding?.Substring(0, Math.Min(100, startup.TeamEmbedding.Length))}...");
                    }

                    if (embeddingUpdated)
                    {
                        await _unitOfWork.StartUps.UpdateAsync(startup);
                        Console.WriteLine("Embeddings saved to database");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error calculating embeddings: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }

            var suggestions = await _embeddingService.GetSuggestedUsersForStartupAsync(startup);

            return Ok(new
            {
                StartupId = startup.Id,
                StartupName = startup.Idea,
                StartupTeam = startup.Team,
                HasTeamEmbedding = !string.IsNullOrEmpty(startup.TeamEmbedding),
                TotalSuggestions = suggestions.Count,
                Suggestions = suggestions
            });
        }

        // POST: api/startups/{id}/recalculate-embeddings
        [HttpPost("{id}/recalculate-embeddings")]
        [Authorize(Roles = "Admin,Client")]
        public async Task<IActionResult> RecalculateStartupEmbeddings(int id)
        {
            var startup = await _unitOfWork.StartUps.GetStartUpWithCategoryAsync(id);

            if (startup == null)
                return NotFound(new { Message = "Startup không tồn tại" });

            try
            {
                // Tính lại TeamEmbedding
                if (!string.IsNullOrEmpty(startup.Team))
                {
                    Console.WriteLine($"Recalculating TeamEmbedding for: {startup.Team}");
                    startup.TeamEmbedding = await _embeddingService.GetEmbeddingAsync(startup.Team);
                }

                await _unitOfWork.StartUps.UpdateAsync(startup);

                return Ok(new
                {
                    Message = "Embeddings đã được tính lại thành công",
                    StartupId = startup.Id,
                    HasTeamEmbedding = !string.IsNullOrEmpty(startup.TeamEmbedding)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error recalculating embeddings: {ex.Message}");
                return StatusCode(500, new { Message = "Lỗi khi tính toán embeddings", Error = ex.Message });
            }
        }

        // GET: api/startups/{id}/suggest-users-grouped
        [HttpGet("{id}/suggest-users-grouped")]
        [Authorize(Roles = "Admin,Client")]
        public async Task<ActionResult<GroupedSuggestionsResponseDto>> SuggestUsersForStartupGrouped(int id)
        {
            var startup = await _unitOfWork.StartUps.GetStartUpWithCategoryAsync(id);

            if (startup == null)
                return NotFound(new { Message = "Startup không tồn tại" });

            Console.WriteLine($"=== Getting grouped suggestions for Startup ID: {id} ===");
            Console.WriteLine($"Startup Team: {startup.Team}");
            Console.WriteLine($"Startup TeamEmbedding exists: {!string.IsNullOrEmpty(startup.TeamEmbedding)}");

            // Nếu chưa có embedding, tính ngay
            bool embeddingUpdated = false;
            if (string.IsNullOrEmpty(startup.TeamEmbedding))
            {
                try
                {
                    if (string.IsNullOrEmpty(startup.TeamEmbedding) && !string.IsNullOrEmpty(startup.Team))
                    {
                        Console.WriteLine("Calculating TeamEmbedding...");
                        startup.TeamEmbedding = await _embeddingService.GetEmbeddingAsync(startup.Team);
                        embeddingUpdated = true;
                        Console.WriteLine($"TeamEmbedding calculated");
                    }

                    if (embeddingUpdated)
                    {
                        await _unitOfWork.StartUps.UpdateAsync(startup);
                        Console.WriteLine("Embeddings saved to database");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error calculating embeddings: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }

            var groupedSuggestions = await _embeddingService.GetGroupedSuggestedUsersForStartupAsync(startup);

            return Ok(new
            {
                StartupId = startup.Id,
                StartupName = startup.Idea,
                StartupTeam = startup.Team,
                HasTeamEmbedding = !string.IsNullOrEmpty(startup.TeamEmbedding),
                GroupedSuggestions = new
                {
                    BySkills = new
                    {
                        Count = groupedSuggestions.BySkills.Count,
                        Users = groupedSuggestions.BySkills
                    },
                    ByRoles = new
                    {
                        Count = groupedSuggestions.ByRoles.Count,
                        Users = groupedSuggestions.ByRoles
                    },
                    ByCategory = new
                    {
                        Count = groupedSuggestions.ByCategory.Count,
                        Users = groupedSuggestions.ByCategory
                    },
                    Overall = new
                    {
                        Count = groupedSuggestions.Overall.Count,
                        Users = groupedSuggestions.Overall
                    }
                }
            });
        }
    }
}
