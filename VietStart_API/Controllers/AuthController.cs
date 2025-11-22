using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietStart_API.Entities.Domains;
using VietStart_API.Entities.DTO;
using VietStart_API.Repositories;
using VietStart_API.Services;

namespace VietStart_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ITokenReposity _tokenRepository;
        private readonly IEmbeddingService _embeddingService;

        public AuthController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, ITokenReposity token, IEmbeddingService embeddingService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenRepository = token;
            _embeddingService = embeddingService;
        }

        [HttpPost]
        [Route("Register-Multi")]
        public async Task<IActionResult> RegisterMulti([FromBody] RegisterMultiRequestDto request)
        {
            var results = new List<object>();

            foreach (var dto in request.Users)
            {
                var user = new AppUser
                {
                    UserName = dto.Email,
                    Email = dto.Email,
                    FullName = dto.FullName,
                    Skills = dto.Skills ?? "",
                    RolesInStartup = dto.RolesInStartup ?? "",
                    CategoryInvests = dto.CategoryInvests ?? "",
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user, dto.Password);

                if (!createResult.Succeeded)
                {
                    results.Add(new
                    {
                        Email = dto.Email,
                        Success = false,
                        Errors = createResult.Errors.Select(e => e.Description)
                    });
                    continue;
                }

                // Gán role
                if (!await _roleManager.RoleExistsAsync("Client"))
                    await _roleManager.CreateAsync(new IdentityRole("Client"));

                await _userManager.AddToRoleAsync(user, "Client");

                // Embedding
                try
                {
                    bool hasEmb = false;

                    if (!string.IsNullOrEmpty(user.Skills))
                    {
                        user.SkillsEmbadding = await _embeddingService.GetEmbeddingAsync(user.Skills);
                        hasEmb = true;
                    }
                    if (!string.IsNullOrEmpty(user.RolesInStartup))
                    {
                        user.RolesEmbadding = await _embeddingService.GetEmbeddingAsync(user.RolesInStartup);
                        hasEmb = true;
                    }
                    if (!string.IsNullOrEmpty(user.CategoryInvests))
                    {
                        user.CategoriesEmbadding = await _embeddingService.GetEmbeddingAsync(user.CategoryInvests);
                        hasEmb = true;
                    }

                    if (hasEmb)
                        await _userManager.UpdateAsync(user);
                }
                catch
                {
                    // lỗi embedding vẫn cho register
                }

                results.Add(new
                {
                    Email = dto.Email,
                    Success = true
                });
            }

            return Ok(results);
        }


        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto requestDto)
        {
            var user = new AppUser
            {
                UserName = requestDto.Email,
                Email = requestDto.Email,
                FullName = requestDto.FullName,
                Skills = requestDto.Skills ?? "",
                RolesInStartup = requestDto.RolesInStartup ?? "",
                CategoryInvests = requestDto.CategoryInvests ?? "",
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, requestDto.Password);

            if (result.Succeeded)
            {
                // Kiểm tra role "Client" có tồn tại chưa
                if (!await _roleManager.RoleExistsAsync("Client"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Client"));
                }

                // Gán role cho user
                await _userManager.AddToRoleAsync(user, "Client");

                // Tính embedding đồng bộ ngay khi đăng ký (nếu có dữ liệu)
                try
                {
                    bool hasEmbeddings = false;

                    if (!string.IsNullOrEmpty(user.Skills))
                    {
                        user.SkillsEmbadding = await _embeddingService.GetEmbeddingAsync(user.Skills);
                        hasEmbeddings = true;
                    }
                    if (!string.IsNullOrEmpty(user.RolesInStartup))
                    {
                        user.RolesEmbadding = await _embeddingService.GetEmbeddingAsync(user.RolesInStartup);
                        hasEmbeddings = true;
                    }
                    if (!string.IsNullOrEmpty(user.CategoryInvests))
                    {
                        user.CategoriesEmbadding = await _embeddingService.GetEmbeddingAsync(user.CategoryInvests);
                        hasEmbeddings = true;
                    }

                    // Chỉ update nếu có embedding được tính
                    if (hasEmbeddings)
                    {
                        await _userManager.UpdateAsync(user);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error calculating embeddings during registration: {ex.Message}");
                    // Vẫn cho phép đăng ký thành công ngay cả khi embedding lỗi
                }

                return Ok(new { Message = "User registered successfully." });
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto requestDto)
        {
            var user = await _userManager.FindByEmailAsync(requestDto.Email);

            if (user != null)
            {
                var result = await _userManager.CheckPasswordAsync(user, requestDto.Password);

                if (result)
                {
                    var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

                    var accessToken = await _tokenRepository.CreateJWTToken(user, role);

                    var refreshToken = await _tokenRepository.GenerateRefreshTokenAsync(user);

                    await _tokenRepository.SaveRefreshTokenAsync(refreshToken);

                    var OAuth2Token = new OAuth2Token
                    {
                        access_token = accessToken,
                        refresh_token = refreshToken.Token,
                        token_type = "Bearer",
                        expires_in = 3600,
                        scope = role
                    };

                    return Ok(OAuth2Token);
                }
            }

            return BadRequest(new { Message = "Invalid email or password." });
        }

        [HttpPost]
        [Route("Refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto request)
        {
            var oldToken = await _tokenRepository.GetRefreshTokenAsync(request.Token);

            if (oldToken == null || oldToken.ExpiresAt <= DateTime.UtcNow || oldToken.IsRevoked == true)
                return Unauthorized("Refresh token không hợp lệ hoặc đã hết hạn.");

            // Sinh token mới
            var user = oldToken.User;
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Client";

            var accessToken = await _tokenRepository.CreateJWTToken(user, role);

            // Thu hồi token cũ
            await _tokenRepository.RevokeRefreshTokenAsync(request.Token);

            var refreshToken = await _tokenRepository.GenerateRefreshTokenAsync(user);

            await _tokenRepository.SaveRefreshTokenAsync(refreshToken);


            var OAuth2Token = new OAuth2Token
            {
                access_token = accessToken,
                refresh_token = refreshToken.Token,
                token_type = "Bearer",
                expires_in = 3600,
                scope = role
            };

            return Ok(OAuth2Token);
        }

        [HttpPost]
        [Route("Logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshRequestDto request)
        {
            await _tokenRepository.RevokeRefreshTokenAsync(request.Token);
            return Ok(new { Message = "Đăng xuất thành công." });
        }

        [HttpGet]
        [Route("Test")]
        [Authorize(Roles = "Client,Admin")]
        public async Task<IActionResult> Test()
        {
            return Ok("API is working!");
        }

    }
}
