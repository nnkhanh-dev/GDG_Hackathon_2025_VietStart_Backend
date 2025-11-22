using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VietStart_API.Entities.DTO;
using VietStart_API.Entities.Domains;
using VietStart_API.Repositories;

namespace VietStart.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeamStartUpsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public TeamStartUpsController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // POST: api/teamstartups/request-join
        // User yêu cầu join vào một startup
        [Authorize(Roles = "Client")]
        [HttpPost("request-join")]
        public async Task<ActionResult> RequestJoinStartup([FromBody] SendJoinRequestDto requestDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var startUp = await _unitOfWork.StartUps.GetByIdAsync(requestDto.StartUpId);
            if (startUp == null)
                return BadRequest(new { Message = "StartUp không tồn tại" });

            if (startUp.UserId == userId)
                return BadRequest(new { Message = "Bạn không thể gửi yêu cầu vào startup của chính mình" });

            var existingRequest = await _unitOfWork.TeamStartUps.FirstOrDefaultAsync(
                t => t.StartUpId == requestDto.StartUpId && 
                     t.UserId == userId &&
                     (t.Status == "Pending" || t.Status == "Accepted"));

            if (existingRequest != null)
            {
                if (existingRequest.Status == "Accepted")
                    return BadRequest(new { Message = "Bạn đã là thành viên của startup này" });
                return BadRequest(new { Message = "Bạn đã gửi yêu cầu tham gia startup này rồi" });
            }

            var teamStartUp = new TeamStartUp
            {
                StartUpId = requestDto.StartUpId,
                UserId = userId,
                Status = "Pending"
            };

            await _unitOfWork.TeamStartUps.AddAsync(teamStartUp);

            return Ok(new { Message = "Gửi yêu cầu tham gia thành công" });
        }

        // POST: api/teamstartups/invite
        // User gửi lời mời cho user khác vào startup của mình
        [Authorize(Roles = "Client")]
        [HttpPost("invite")]
        public async Task<ActionResult> InviteUserToStartup([FromBody] CreateTeamStartUpDto inviteDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ownerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(ownerId))
                return Unauthorized();

            var startUp = await _unitOfWork.StartUps.GetByIdAsync(inviteDto.StartUpId);
            if (startUp == null)
                return BadRequest(new { Message = "StartUp không tồn tại" });

            if (startUp.UserId != ownerId)
                return Forbid();

            var user = await _unitOfWork.Users.GetByIdAsync(inviteDto.UserId);
            if (user == null)
                return BadRequest(new { Message = "User không tồn tại" });

            var existingMember = await _unitOfWork.TeamStartUps.FirstOrDefaultAsync(
                t => t.StartUpId == inviteDto.StartUpId && 
                     t.UserId == inviteDto.UserId &&
                     (t.Status == "Pending" || t.Status == "Accepted"));

            if (existingMember != null)
            {
                if (existingMember.Status == "Accepted")
                    return BadRequest(new { Message = "User đã là thành viên của startup này" });
                return BadRequest(new { Message = "User đã có lời mời từ startup này" });
            }

            var teamStartUp = new TeamStartUp
            {
                StartUpId = inviteDto.StartUpId,
                UserId = inviteDto.UserId,
                Status = inviteDto.Status ?? "Pending"
            };

            await _unitOfWork.TeamStartUps.AddAsync(teamStartUp);

            return Ok(new { Message = "Gửi lời mời thành công" });
        }

        // GET: api/teamstartups/pending-requests
        // Lấy danh sách yêu cầu join đang chờ cho các startup của mình
        [Authorize(Roles = "Client")]
        [HttpGet("pending-requests")]
        public async Task<ActionResult<IEnumerable<TeamStartUpDto>>> GetPendingRequests([FromQuery] int? startUpId = null)
        {
            var ownerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(ownerId))
                return Unauthorized();

            IEnumerable<TeamStartUp> pendingRequests;

            if (startUpId.HasValue)
            {
                var startUp = await _unitOfWork.StartUps.GetByIdAsync(startUpId.Value);
                if (startUp == null)
                    return NotFound(new { Message = "StartUp không tồn tại" });

                if (startUp.UserId != ownerId)
                    return Forbid();

                pendingRequests = await _unitOfWork.TeamStartUps.GetPendingRequestsByStartUpIdAsync(startUpId.Value);
            }
            else
            {
                // Lấy tất cả startup của owner
                var myStartups = await _unitOfWork.StartUps.GetAllAsync(s => s.UserId == ownerId);
                var startupIds = myStartups.Select(s => s.Id).ToList();

                // Lấy tất cả pending requests cho các startup đó
                var allRequests = await _unitOfWork.TeamStartUps.GetTeamStartUpsByStatusAsync("Pending");
                pendingRequests = allRequests.Where(t => startupIds.Contains(t.StartUpId));
            }

            var requestDtos = pendingRequests.Select(t => new TeamStartUpDto
            {
                Id = t.Id,
                StartUpId = t.StartUpId,
                StartUpIdea = t.StartUp?.Idea ?? "",
                UserId = t.UserId,
                UserFullName = t.User?.FullName ?? "",
                UserAvatar = t.User?.Avatar,
                Status = t.Status
            }).ToList();

            return Ok(new { Data = requestDtos, Total = requestDtos.Count });
        }

        // PUT: api/teamstartups/{id}/accept
        // Chấp nhận người đã gửi yêu cầu vào startup của mình
        [Authorize(Roles = "Client")]
        [HttpPut("{id}/accept")]
        public async Task<IActionResult> AcceptRequest(int id)
        {
            var ownerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(ownerId))
                return Unauthorized();

            var teamStartUp = await _unitOfWork.TeamStartUps.GetByIdAsync(id);
            if (teamStartUp == null)
                return NotFound(new { Message = "Yêu cầu không tồn tại" });

            var startUp = await _unitOfWork.StartUps.GetByIdAsync(teamStartUp.StartUpId);
            if (startUp?.UserId != ownerId)
                return Forbid();

            if (teamStartUp.Status != "Pending")
                return BadRequest(new { Message = "Yêu cầu này đã được xử lý" });

            teamStartUp.Status = "Accepted";
            await _unitOfWork.TeamStartUps.UpdateAsync(teamStartUp);

            return Ok(new { Message = "Đã chấp nhận yêu cầu tham gia" });
        }

        // PUT: api/teamstartups/{id}/reject
        // Từ chối người đã gửi yêu cầu vào startup của mình
        [Authorize(Roles = "Client")]
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> RejectRequest(int id, [FromBody] UpdateJoinRequestStatusDto? rejectDto = null)
        {
            var ownerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(ownerId))
                return Unauthorized();

            var teamStartUp = await _unitOfWork.TeamStartUps.GetByIdAsync(id);
            if (teamStartUp == null)
                return NotFound(new { Message = "Yêu cầu không tồn tại" });

            var startUp = await _unitOfWork.StartUps.GetByIdAsync(teamStartUp.StartUpId);
            if (startUp?.UserId != ownerId)
                return Forbid();

            if (teamStartUp.Status != "Pending")
                return BadRequest(new { Message = "Yêu cầu này đã được xử lý" });

            teamStartUp.Status = "Rejected";
            await _unitOfWork.TeamStartUps.UpdateAsync(teamStartUp);

            return Ok(new { Message = "Đã từ chối yêu cầu tham gia", Reason = rejectDto?.Reason });
        }

        // GET: api/teamstartups/my-startups-members
        // Lấy danh sách user của một hoặc nhiều startup của mình
        [Authorize(Roles = "Client")]
        [HttpGet("my-startups-members")]
        public async Task<ActionResult<IEnumerable<TeamStartUpDto>>> GetMyStartupsMembers(
            [FromQuery] int? startUpId = null,
            [FromQuery] string? status = "Accepted")
        {
            var ownerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(ownerId))
                return Unauthorized();

            IEnumerable<TeamStartUp> members;

            if (startUpId.HasValue)
            {
                var startUp = await _unitOfWork.StartUps.GetByIdAsync(startUpId.Value);
                if (startUp == null)
                    return NotFound(new { Message = "StartUp không tồn tại" });

                if (startUp.UserId != ownerId)
                    return Forbid();

                members = await _unitOfWork.TeamStartUps.GetTeamStartUpsByStartUpIdAsync(startUpId.Value);
            }
            else
            {
                var myStartups = await _unitOfWork.StartUps.GetAllAsync(s => s.UserId == ownerId);
                var startupIds = myStartups.Select(s => s.Id).ToList();

                var allMembers = await _unitOfWork.TeamStartUps.GetAllAsync(t => startupIds.Contains(t.StartUpId));
                members = allMembers;
            }

            if (!string.IsNullOrEmpty(status))
                members = members.Where(t => t.Status == status);

            var memberDtos = members.Select(t => new TeamStartUpDto
            {
                Id = t.Id,
                StartUpId = t.StartUpId,
                StartUpIdea = t.StartUp?.Idea ?? "",
                UserId = t.UserId,
                UserFullName = t.User?.FullName ?? "",
                UserAvatar = t.User?.Avatar,
                Status = t.Status
            }).ToList();

            return Ok(new { Data = memberDtos, Total = memberDtos.Count });
        }

        // DELETE: api/teamstartups/{id}/remove
        // Xóa thành viên khỏi startup của mình
        [Authorize(Roles = "Client")]
        [HttpDelete("{id}/remove")]
        public async Task<IActionResult> RemoveMember(int id)
        {
            var ownerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(ownerId))
                return Unauthorized();

            var teamStartUp = await _unitOfWork.TeamStartUps.GetByIdAsync(id);
            if (teamStartUp == null)
                return NotFound(new { Message = "Thành viên không tồn tại" });

            var startUp = await _unitOfWork.StartUps.GetByIdAsync(teamStartUp.StartUpId);
            if (startUp?.UserId != ownerId)
                return Forbid();

            await _unitOfWork.TeamStartUps.DeleteAsync(teamStartUp);

            return Ok(new { Message = "Đã xóa thành viên khỏi startup" });
        }

        // GET: api/teamstartups/my-requests
        // Xem các yêu cầu tham gia đã gửi của mình
        [Authorize(Roles = "Client")]
        [HttpGet("my-requests")]
        public async Task<ActionResult<IEnumerable<TeamStartUpDto>>> GetMyRequests([FromQuery] string? status = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var requests = await _unitOfWork.TeamStartUps.GetTeamStartUpsByUserIdAsync(userId);

            if (!string.IsNullOrEmpty(status))
                requests = requests.Where(t => t.Status == status);

            var requestDtos = requests.Select(t => new TeamStartUpDto
            {
                Id = t.Id,
                StartUpId = t.StartUpId,
                StartUpIdea = t.StartUp?.Idea ?? "",
                UserId = t.UserId,
                UserFullName = t.User?.FullName ?? "",
                UserAvatar = t.User?.Avatar,
                Status = t.Status
            }).ToList();

            return Ok(new { Data = requestDtos, Total = requestDtos.Count });
        }

        // DELETE: api/teamstartups/{id}/cancel-request
        // Hủy yêu cầu tham gia đã gửi
        [Authorize(Roles = "Client")]
        [HttpDelete("{id}/cancel-request")]
        public async Task<IActionResult> CancelRequest(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var teamStartUp = await _unitOfWork.TeamStartUps.GetByIdAsync(id);
            if (teamStartUp == null)
                return NotFound(new { Message = "Yêu cầu không tồn tại" });

            if (teamStartUp.UserId != userId)
                return Forbid();

            if (teamStartUp.Status != "Pending")
                return BadRequest(new { Message = "Chỉ có thể hủy yêu cầu đang chờ xử lý" });

            await _unitOfWork.TeamStartUps.DeleteAsync(teamStartUp);

            return Ok(new { Message = "Đã hủy yêu cầu tham gia" });
        }
    }
}
