using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    public class UsersController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMapper _mapper;
        private readonly IEmbeddingService _embeddingService;

        public UsersController(IUnitOfWork unitOfWork, UserManager<AppUser> userManager, IMapper mapper, IEmbeddingService embeddingService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _mapper = mapper;
            _embeddingService = embeddingService;
        }

        // GET: api/users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<AppUserDto>> GetUser(string id)
        {
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

            if (user == null)
                return NotFound(new { Message = "Người dùng không tồn tại" });

            var userDto = _mapper.Map<AppUserDto>(user);

            return Ok(userDto);
        }

        // GET: api/users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AppUserDto>>> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var (users, total) = await _unitOfWork.Users.GetPaginatedAsync(
                page,
                pageSize,
                u => u.DeletedAt == null,
                q => q.OrderBy(u => u.CreatedAt));

            var userDtos = _mapper.Map<IEnumerable<AppUserDto>>(users);

            return Ok(new { Data = userDtos, Total = total });
        }

        // POST: api/users/{id}/update
        [Authorize(Roles = "Admin,Client")]
        [HttpPost("{id}/update")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateAppUserDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (id != currentUserId)
                return Forbid();

            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

            if (user == null)
                return NotFound(new { Message = "Người dùng không tồn tại" });

            user.FullName = updateDto.FullName ?? user.FullName;
            user.Location = updateDto.Location ?? user.Location;
            user.Bio = updateDto.Bio ?? user.Bio;
            user.Avatar = updateDto.Avatar ?? user.Avatar;
            if (updateDto.DOB.HasValue)
            {
                user.DOB = updateDto.DOB.Value;
            }
            
            // Cập nhật Skills, Roles, Categories nếu có trong DTO
            bool needRecalculateEmbeddings = false;
            
            if (updateDto.Skills != null)
            {
                user.Skills = updateDto.Skills;
                needRecalculateEmbeddings = true;
            }
            
            if (updateDto.RolesInStartup != null)
            {
                user.RolesInStartup = updateDto.RolesInStartup;
                needRecalculateEmbeddings = true;
            }
            
            if (updateDto.CategoryInvests != null)
            {
                user.CategoryInvests = updateDto.CategoryInvests;
                needRecalculateEmbeddings = true;
            }
            
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = currentUserId;

            // Tính lại embedding nếu có thay đổi Skills, Roles hoặc Categories
            if (needRecalculateEmbeddings)
            {
                try
                {
                    if (!string.IsNullOrEmpty(user.Skills))
                    {
                        Console.WriteLine($"Calculating SkillsEmbedding: {user.Skills}");
                        user.SkillsEmbadding = await _embeddingService.GetEmbeddingAsync(user.Skills);
                    }
                    else
                    {
                        user.SkillsEmbadding = null;
                    }
                    
                    if (!string.IsNullOrEmpty(user.RolesInStartup))
                    {
                        Console.WriteLine($"Calculating RolesEmbedding: {user.RolesInStartup}");
                        user.RolesEmbadding = await _embeddingService.GetEmbeddingAsync(user.RolesInStartup);
                    }
                    else
                    {
                        user.RolesEmbadding = null;
                    }
                    
                    if (!string.IsNullOrEmpty(user.CategoryInvests))
                    {
                        Console.WriteLine($"Calculating CategoriesEmbedding: {user.CategoryInvests}");
                        user.CategoriesEmbadding = await _embeddingService.GetEmbeddingAsync(user.CategoryInvests);
                    }
                    else
                    {
                        user.CategoriesEmbadding = null;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error calculating user embeddings: {ex.Message}");
                    // Tiếp tục update user ngay cả khi embedding lỗi
                }
            }

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { Message = "Cập nhật thông tin người dùng thành công" });
        }

        // GET: api/users/{id}/startups
        [HttpGet("{id}/startups")]
        public async Task<ActionResult<IEnumerable<StartUpDto>>> GetUserStartups(string id)
        {
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);
            if (user == null)
                return NotFound(new { Message = "Người dùng không tồn tại" });

            var startups = await _unitOfWork.StartUps.GetUserStartupsAsync(id);

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
                UserId = s.UserId,
                UserFullName = s.AppUser.FullName,
                CategoryId = s.CategoryId,
                CategoryName = s.Category.Name,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            }).ToList();

            return Ok(startupDtos);
        }

        // GET: api/users/search/{keyword}
        [HttpGet("search/{keyword}")]
        public async Task<ActionResult<IEnumerable<AppUserDto>>> SearchUsers(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return BadRequest(new { Message = "Từ khóa tìm kiếm không được trống" });

            var users = await _unitOfWork.Users.SearchUsersAsync(keyword);

            var userDtos = _mapper.Map<IEnumerable<AppUserDto>>(users);

            return Ok(userDtos);
        }

        // GET: api/users/{id}/profile
        [HttpGet("{id}/profile")]
        public async Task<ActionResult<dynamic>> GetUserProfile(string id)
        {
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

            if (user == null)
                return NotFound(new { Message = "Người dùng không tồn tại" });

            var startupsCount = await _unitOfWork.StartUps.CountAsync(s => s.UserId == id && s.DeletedAt == null);
            var commentsCount = await _unitOfWork.Comments.CountAsync(c => c.UserId == id && c.DeletedAt == null);
            var sharesCount = await _unitOfWork.Shares.CountAsync(s => s.UserId == id && s.DeletedAt == null);

            return Ok(new
            {
                User = _mapper.Map<AppUserDto>(user),
                Statistics = new
                {
                    StartupsCount = startupsCount,
                    CommentsCount = commentsCount,
                    SharesCount = sharesCount
                }
            });
        }

        // POST: api/users/{id}/delete
        [Authorize(Roles = "Admin,Client")]
        [HttpPost("{id}/delete")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (id != currentUserId)
                return Forbid();

            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

            if (user == null)
                return NotFound(new { Message = "Người dùng không tồn tại" });

            user.DeletedAt = DateTime.UtcNow;
            user.DeletedBy = currentUserId;

            await _unitOfWork.Users.UpdateAsync(user);

            return Ok(new { Message = "Xóa tài khoản thành công" });
        }

        // POST: api/users/{id}/recalculate-embeddings
        [HttpPost("{id}/recalculate-embeddings")]
        [Authorize(Roles = "Admin,Client")]
        public async Task<IActionResult> RecalculateUserEmbeddings(string id)
        {
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

            if (user == null)
                return NotFound(new { Message = "Người dùng không tồn tại" });

            try
            {
                Console.WriteLine($"=== Recalculating embeddings for User: {user.FullName} ({user.Email}) ===");

                // Tính lại SkillsEmbedding
                if (!string.IsNullOrEmpty(user.Skills))
                {
                    Console.WriteLine($"Skills: {user.Skills}");
                    user.SkillsEmbadding = await _embeddingService.GetEmbeddingAsync(user.Skills);
                    Console.WriteLine($"SkillsEmbedding calculated: {user.SkillsEmbadding?.Substring(0, Math.Min(50, user.SkillsEmbadding.Length))}...");
                }
                else
                {
                    Console.WriteLine("Skills is empty, skipping");
                }

                // Tính lại RolesEmbedding
                if (!string.IsNullOrEmpty(user.RolesInStartup))
                {
                    Console.WriteLine($"RolesInStartup: {user.RolesInStartup}");
                    user.RolesEmbadding = await _embeddingService.GetEmbeddingAsync(user.RolesInStartup);
                    Console.WriteLine($"RolesEmbedding calculated: {user.RolesEmbadding?.Substring(0, Math.Min(50, user.RolesEmbadding.Length ))}...");
                }
                else
                {
                    Console.WriteLine("RolesInStartup is empty, skipping");
                }

                // Tính lại CategoriesEmbedding
                if (!string.IsNullOrEmpty(user.CategoryInvests))
                {
                    Console.WriteLine($"CategoryInvests: {user.CategoryInvests}");
                    user.CategoriesEmbadding = await _embeddingService.GetEmbeddingAsync(user.CategoryInvests);
                    Console.WriteLine($"CategoriesEmbedding calculated: {user.CategoriesEmbadding?.Substring(0, Math.Min(50, user.CategoriesEmbadding.Length ))}...");
                }
                else
                {
                    Console.WriteLine("CategoryInvests is empty, skipping");
                }

                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    Console.WriteLine($"Failed to update user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    return BadRequest(result.Errors);
                }

                Console.WriteLine("Embeddings saved successfully");

                return Ok(new
                {
                    Message = "Embeddings đã được tính lại thành công",
                    UserId = user.Id,
                    UserName = user.FullName,
                    HasSkillsEmbedding = !string.IsNullOrEmpty(user.SkillsEmbadding),
                    HasRolesEmbedding = !string.IsNullOrEmpty(user.RolesEmbadding),
                    HasCategoriesEmbedding = !string.IsNullOrEmpty(user.CategoriesEmbadding)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error recalculating user embeddings: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { Message = "Lỗi khi tính toán embeddings", Error = ex.Message });
            }
        }

        // POST: api/users/recalculate-all-embeddings
        [HttpPost("recalculate-all-embeddings")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RecalculateAllUserEmbeddings()
        {
            try
            {
                var users = await _unitOfWork.Users.GetAllAsync(u => u.DeletedAt == null);
                var usersList = users.ToList();

                Console.WriteLine($"=== Recalculating embeddings for {usersList.Count} users ===");

                int successCount = 0;
                int errorCount = 0;
                var results = new List<object>();

                foreach (var user in usersList)
                {
                    try
                    {
                        Console.WriteLine($"\nProcessing user: {user.FullName} ({user.Email})");

                        bool hasChanges = false;

                        // Tính SkillsEmbedding
                        if (!string.IsNullOrEmpty(user.Skills) && string.IsNullOrEmpty(user.SkillsEmbadding))
                        {
                            Console.WriteLine($"Calculating Skills embedding: {user.Skills}");
                            user.SkillsEmbadding = await _embeddingService.GetEmbeddingAsync(user.Skills);
                            hasChanges = true;
                        }

                        // Tính RolesEmbedding
                        if (!string.IsNullOrEmpty(user.RolesInStartup) && string.IsNullOrEmpty(user.RolesEmbadding))
                        {
                            Console.WriteLine($"Calculating Roles embedding: {user.RolesInStartup}");
                            user.RolesEmbadding = await _embeddingService.GetEmbeddingAsync(user.RolesInStartup);
                            hasChanges = true;
                        }

                        // Tính CategoriesEmbedding
                        if (!string.IsNullOrEmpty(user.CategoryInvests) && string.IsNullOrEmpty(user.CategoriesEmbadding))
                        {
                            Console.WriteLine($"Calculating Categories embedding: {user.CategoryInvests}");
                            user.CategoriesEmbadding = await _embeddingService.GetEmbeddingAsync(user.CategoryInvests);
                            hasChanges = true;
                        }

                        if (hasChanges)
                        {
                            var result = await _userManager.UpdateAsync(user);
                            if (result.Succeeded)
                            {
                                successCount++;
                                Console.WriteLine($"✓ User {user.FullName} updated successfully");
                                results.Add(new
                                {
                                    UserId = user.Id,
                                    UserName = user.FullName,
                                    Status = "Success",
                                    HasSkillsEmbedding = !string.IsNullOrEmpty(user.SkillsEmbadding),
                                    HasRolesEmbedding = !string.IsNullOrEmpty(user.RolesEmbadding),
                                    HasCategoriesEmbedding = !string.IsNullOrEmpty(user.CategoriesEmbadding)
                                });
                            }
                            else
                            {
                                errorCount++;
                                Console.WriteLine($"✗ Failed to update user {user.FullName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                                results.Add(new
                                {
                                    UserId = user.Id,
                                    UserName = user.FullName,
                                    Status = "Failed",
                                    Error = string.Join(", ", result.Errors.Select(e => e.Description))
                                });
                            }
                        }
                        else
                        {
                            Console.WriteLine($"⊘ No changes needed for user {user.FullName}");
                            results.Add(new
                            {
                                UserId = user.Id,
                                UserName = user.FullName,
                                Status = "Skipped",
                                Reason = "No data to embed or embeddings already exist"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        Console.WriteLine($"✗ Error processing user {user.FullName}: {ex.Message}");
                        results.Add(new
                        {
                            UserId = user.Id,
                            UserName = user.FullName,
                            Status = "Error",
                            Error = ex.Message
                        });
                    }
                }

                Console.WriteLine($"\n=== Completed: {successCount} success, {errorCount} errors ===");

                return Ok(new
                {
                    Message = "Hoàn thành tính toán embeddings",
                    TotalUsers = usersList.Count,
                    SuccessCount = successCount,
                    ErrorCount = errorCount,
                    Results = results
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RecalculateAllUserEmbeddings: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { Message = "Lỗi khi tính toán embeddings", Error = ex.Message });
            }
        }
    }
}
