using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VietStart_API.Entities.DTO;
using VietStart_API.Entities.Domains;
using VietStart_API.Repositories;
using VietStart_API.Enums;

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

        // POST: api/teamstartups/invite
        // Chủ startup gửi lời mời chiêu mộ cho user khác
        [Authorize(Roles = "Client")]
        [HttpPost("invite")]
        public async Task<ActionResult> InviteUserToStartup([FromBody] CreateTeamStartUpDto inviteDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Message = "Dữ liệu không hợp lệ", Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });

            var ownerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(ownerId))
                return Unauthorized(new { Message = "Không xác định được người dùng" });

            var startUp = await _unitOfWork.StartUps.GetByIdAsync(inviteDto.StartUpId);
            if (startUp == null)
                return BadRequest(new { Message = "StartUp không tồn tại" });

            if (startUp.UserId != ownerId)
                return StatusCode(403, new { Message = "Bạn không có quyền gửi lời mời cho startup này" });

            var user = await _unitOfWork.Users.GetByIdAsync(inviteDto.UserId);
            if (user == null)
                return BadRequest(new { Message = "User không tồn tại" });

            // Kiểm tra duplicate invitation
            var existingInvite = await _unitOfWork.TeamStartUps.FirstOrDefaultAsync(
                t => t.StartUpId == inviteDto.StartUpId && 
                     t.UserId == inviteDto.UserId &&
                     (t.Status == TeamStartUpStatus.Pending || 
                      t.Status == TeamStartUpStatus.Dealing || 
                      t.Status == TeamStartUpStatus.Success));

            if (existingInvite != null)
            {
                if (existingInvite.Status == TeamStartUpStatus.Success)
                    return BadRequest(new { Message = "User đã là thành viên của startup này" });
                if (existingInvite.Status == TeamStartUpStatus.Dealing)
                    return BadRequest(new { Message = "Đang trong quá trình trao đổi với user này" });
                return BadRequest(new { Message = "Đã có lời mời đang chờ xử lý" });
            }

            var teamStartUp = new TeamStartUp
            {
                StartUpId = inviteDto.StartUpId,
                UserId = inviteDto.UserId,
                Status = TeamStartUpStatus.Pending
            };

            await _unitOfWork.TeamStartUps.AddAsync(teamStartUp);

            return Ok(new { Message = "Gửi lời mời chiêu mộ thành công" });
        }

        // GET: api/teamstartups/sent-invites
        // Lấy danh sách lời mời đã gửi của các startup của mình (Người gửi lời mời)
        [Authorize(Roles = "Client")]
        [HttpGet("sent-invites")]
        public async Task<ActionResult<IEnumerable<TeamStartUpDto>>> GetSentInvites(
            [FromQuery] int? startUpId = null,
            [FromQuery] TeamStartUpStatus? status = null)
        {
            var ownerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(ownerId))
                return Unauthorized(new { Message = "Không xác định được người dùng" });

            IEnumerable<TeamStartUp> invites;

            if (startUpId.HasValue)
            {
                var startUp = await _unitOfWork.StartUps.GetByIdAsync(startUpId.Value);
                if (startUp == null)
                    return NotFound(new { Message = "StartUp không tồn tại" });

                if (startUp.UserId != ownerId)
                    return StatusCode(403, new { Message = "Bạn không có quyền xem lời mời của startup này" });

                invites = await _unitOfWork.TeamStartUps.GetTeamStartUpsByStartUpIdAsync(startUpId.Value);
            }
            else
            {
                var myStartups = await _unitOfWork.StartUps.GetAllAsync(s => s.UserId == ownerId);
                var startupIds = myStartups.Select(s => s.Id).ToList();
                var allInvites = await _unitOfWork.TeamStartUps.GetAllAsync(t => startupIds.Contains(t.StartUpId));
                invites = allInvites;
            }

            if (status.HasValue)
                invites = invites.Where(t => t.Status == status.Value);

            var owner = await _unitOfWork.Users.GetByIdAsync(ownerId);
            
            var inviteDtos = invites.Select(t => new TeamStartUpDto
            {
                Id = t.Id,
                StartUpId = t.StartUpId,
                StartUpIdea = t.StartUp?.Idea ?? "",
                UserId = t.UserId,
                UserFullName = t.User?.FullName ?? "",
                UserAvatar = t.User?.Avatar,
                Status = t.Status,
                StartupOwnerId = ownerId,
                StartupOwnerName = owner?.FullName ?? "",
                StartupOwnerAvatar = owner?.Avatar
            }).ToList();

            return Ok(new { Data = inviteDtos, Total = inviteDtos.Count });
        }

        // GET: api/teamstartups/received-invites
        // Lấy danh sách lời mời nhận được (Người được gửi lời mời)
        [Authorize(Roles = "Client")]
        [HttpGet("received-invites")]
        public async Task<ActionResult<IEnumerable<TeamStartUpDto>>> GetReceivedInvites([FromQuery] TeamStartUpStatus? status = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Không xác định được người dùng" });

            var invites = await _unitOfWork.TeamStartUps.GetTeamStartUpsByUserIdAsync(userId);

            if (status.HasValue)
                invites = invites.Where(t => t.Status == status.Value);

            var inviteDtos = new List<TeamStartUpDto>();
            
            foreach (var t in invites)
            {
                var startUp = t.StartUp ?? await _unitOfWork.StartUps.GetByIdAsync(t.StartUpId);
                var owner = startUp != null ? await _unitOfWork.Users.GetByIdAsync(startUp.UserId) : null;
                
                inviteDtos.Add(new TeamStartUpDto
                {
                    Id = t.Id,
                    StartUpId = t.StartUpId,
                    StartUpIdea = startUp?.Idea ?? "",
                    UserId = t.UserId,
                    UserFullName = t.User?.FullName ?? "",
                    UserAvatar = t.User?.Avatar,
                    Status = t.Status,
                    StartupOwnerId = startUp?.UserId ?? "",
                    StartupOwnerName = owner?.FullName ?? "",
                    StartupOwnerAvatar = owner?.Avatar
                });
            }

            return Ok(new { Data = inviteDtos, Total = inviteDtos.Count });
        }

        // PUT: api/teamstartups/{id}/accept-invite
        // Người được mời đồng ý lời mời → chuyển sang trạng thái Dealing (bắt đầu nhắn tin)
        [Authorize(Roles = "Client")]
        [HttpPut("{id}/accept-invite")]
        public async Task<IActionResult> AcceptInvite(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Không xác định được người dùng" });

            var teamStartUp = await _unitOfWork.TeamStartUps.GetByIdAsync(id);
            if (teamStartUp == null)
                return NotFound(new { Message = "Lời mời không tồn tại" });

            if (teamStartUp.UserId != userId)
                return StatusCode(403, new { Message = "Bạn không có quyền chấp nhận lời mời này" });

            if (teamStartUp.Status != TeamStartUpStatus.Pending)
                return BadRequest(new { Message = "Lời mời này không ở trạng thái chờ xử lý" });

            teamStartUp.Status = TeamStartUpStatus.Dealing;
            await _unitOfWork.TeamStartUps.UpdateAsync(teamStartUp);

            return Ok(new { 
                Message = "Đã chấp nhận lời mời. Bây giờ bạn có thể nhắn tin trao đổi với chủ startup",
                Status = (int)TeamStartUpStatus.Dealing 
            });
        }

        // PUT: api/teamstartups/{id}/reject-invite
        // Người được mời từ chối lời mời
        [Authorize(Roles = "Client")]
        [HttpPut("{id}/reject-invite")]
        public async Task<IActionResult> RejectInvite(int id, [FromBody] UpdateJoinRequestStatusDto? rejectDto = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Không xác định được người dùng" });

            var teamStartUp = await _unitOfWork.TeamStartUps.GetByIdAsync(id);
            if (teamStartUp == null)
                return NotFound(new { Message = "Lời mời không tồn tại" });

            if (teamStartUp.UserId != userId)
                return StatusCode(403, new { Message = "Bạn không có quyền từ chối lời mời này" });

            if (teamStartUp.Status != TeamStartUpStatus.Pending)
                return BadRequest(new { Message = "Lời mời này không ở trạng thái chờ xử lý" });

            teamStartUp.Status = TeamStartUpStatus.Rejected;
            await _unitOfWork.TeamStartUps.UpdateAsync(teamStartUp);

            return Ok(new { 
                Message = "Đã từ chối lời mời", 
                Reason = rejectDto?.Reason,
                Status = (int)TeamStartUpStatus.Rejected 
            });
        }

        // PUT: api/teamstartups/{id}/confirm-success
        // Chủ startup xác nhận thành công → người được mời vào nhóm chat chung
        [Authorize(Roles = "Client")]
        [HttpPut("{id}/confirm-success")]
        public async Task<IActionResult> ConfirmSuccess(int id)
        {
            var ownerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(ownerId))
                return Unauthorized(new { Message = "Không xác định được người dùng" });

            var teamStartUp = await _unitOfWork.TeamStartUps.GetByIdAsync(id);
            if (teamStartUp == null)
                return NotFound(new { Message = "Lời mời không tồn tại" });

            var startUp = await _unitOfWork.StartUps.GetByIdAsync(teamStartUp.StartUpId);
            if (startUp?.UserId != ownerId)
                return StatusCode(403, new { Message = "Bạn không có quyền xác nhận lời mời này" });

            if (teamStartUp.Status != TeamStartUpStatus.Dealing)
                return BadRequest(new { Message = "Chỉ có thể xác nhận thành công khi đang ở trạng thái Dealing" });

            teamStartUp.Status = TeamStartUpStatus.Success;
            await _unitOfWork.TeamStartUps.UpdateAsync(teamStartUp);

            // TODO: Thêm logic để thêm user vào nhóm chat chung của startup
            // Frontend sẽ tự tạo group chat room trên Firebase

            return Ok(new { 
                Message = "Đã xác nhận thành công. Thành viên đã được thêm vào nhóm chat",
                Status = (int)TeamStartUpStatus.Success 
            });
        }

        // PUT: api/teamstartups/{id}/cancel-dealing
        // Chủ startup hủy bỏ quá trình trao đổi và chuyển về Rejected
        [Authorize(Roles = "Client")]
        [HttpPut("{id}/cancel-dealing")]
        public async Task<IActionResult> CancelDealing(int id, [FromBody] UpdateJoinRequestStatusDto? cancelDto = null)
        {
            var ownerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(ownerId))
                return Unauthorized(new { Message = "Không xác định được người dùng" });

            var teamStartUp = await _unitOfWork.TeamStartUps.GetByIdAsync(id);
            if (teamStartUp == null)
                return NotFound(new { Message = "Lời mời không tồn tại" });

            var startUp = await _unitOfWork.StartUps.GetByIdAsync(teamStartUp.StartUpId);
            if (startUp?.UserId != ownerId)
                return StatusCode(403, new { Message = "Bạn không có quyền hủy lời mời này" });

            if (teamStartUp.Status != TeamStartUpStatus.Dealing)
                return BadRequest(new { Message = "Chỉ có thể hủy khi đang ở trạng thái Dealing" });

            teamStartUp.Status = TeamStartUpStatus.Rejected;
            await _unitOfWork.TeamStartUps.UpdateAsync(teamStartUp);

            return Ok(new { 
                Message = "Đã hủy bỏ quá trình trao đổi", 
                Reason = cancelDto?.Reason,
                Status = (int)TeamStartUpStatus.Rejected 
            });
        }

        // GET: api/teamstartups/my-team-members
        // Lấy danh sách thành viên đã thành công (Success) của startup
        [Authorize(Roles = "Client")]
        [HttpGet("my-team-members")]
        public async Task<ActionResult<IEnumerable<TeamStartUpDto>>> GetMyTeamMembers([FromQuery] int? startUpId = null)
        {
            var ownerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(ownerId))
                return Unauthorized(new { Message = "Không xác định được người dùng" });

            IEnumerable<TeamStartUp> members;

            if (startUpId.HasValue)
            {
                var startUp = await _unitOfWork.StartUps.GetByIdAsync(startUpId.Value);
                if (startUp == null)
                    return NotFound(new { Message = "StartUp không tồn tại" });

                if (startUp.UserId != ownerId)
                    return StatusCode(403, new { Message = "Bạn không có quyền xem thành viên của startup này" });

                members = await _unitOfWork.TeamStartUps.GetTeamStartUpsByStartUpIdAsync(startUpId.Value);
            }
            else
            {
                var myStartups = await _unitOfWork.StartUps.GetAllAsync(s => s.UserId == ownerId);
                var startupIds = myStartups.Select(s => s.Id).ToList();
                var allMembers = await _unitOfWork.TeamStartUps.GetAllAsync(t => startupIds.Contains(t.StartUpId));
                members = allMembers;
            }

            members = members.Where(t => t.Status == TeamStartUpStatus.Success);

            var owner = await _unitOfWork.Users.GetByIdAsync(ownerId);

            var memberDtos = members.Select(t => new TeamStartUpDto
            {
                Id = t.Id,
                StartUpId = t.StartUpId,
                StartUpIdea = t.StartUp?.Idea ?? "",
                UserId = t.UserId,
                UserFullName = t.User?.FullName ?? "",
                UserAvatar = t.User?.Avatar,
                Status = t.Status,
                StartupOwnerId = ownerId,
                StartupOwnerName = owner?.FullName ?? "",
                StartupOwnerAvatar = owner?.Avatar
            }).ToList();

            return Ok(new { Data = memberDtos, Total = memberDtos.Count });
        }

        // DELETE: api/teamstartups/{id}/remove-member
        // Chủ startup xóa thành viên khỏi nhóm
        [Authorize(Roles = "Client")]
        [HttpDelete("{id}/remove-member")]
        public async Task<IActionResult> RemoveMember(int id)
        {
            var ownerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(ownerId))
                return Unauthorized(new { Message = "Không xác định được người dùng" });

            var teamStartUp = await _unitOfWork.TeamStartUps.GetByIdAsync(id);
            if (teamStartUp == null)
                return NotFound(new { Message = "Thành viên không tồn tại" });

            var startUp = await _unitOfWork.StartUps.GetByIdAsync(teamStartUp.StartUpId);
            if (startUp?.UserId != ownerId)
                return StatusCode(403, new { Message = "Bạn không có quyền xóa thành viên này" });

            // TODO: Thêm logic để xóa user khỏi nhóm chat (hoặc để frontend làm)

            await _unitOfWork.TeamStartUps.DeleteAsync(teamStartUp);

            return Ok(new { Message = "Đã xóa thành viên khỏi startup" });
        }

        // DELETE: api/teamstartups/{id}/cancel-invite
        // Chủ startup hủy lời mời đã gửi (khi còn ở trạng thái Pending)
        [Authorize(Roles = "Client")]
        [HttpDelete("{id}/cancel-invite")]
        public async Task<IActionResult> CancelInvite(int id)
        {
            var ownerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(ownerId))
                return Unauthorized(new { Message = "Không xác định được người dùng" });

            var teamStartUp = await _unitOfWork.TeamStartUps.GetByIdAsync(id);
            if (teamStartUp == null)
                return NotFound(new { Message = "Lời mời không tồn tại" });

            var startUp = await _unitOfWork.StartUps.GetByIdAsync(teamStartUp.StartUpId);
            if (startUp?.UserId != ownerId)
                return StatusCode(403, new { Message = "Bạn không có quyền hủy lời mời này" });

            if (teamStartUp.Status != TeamStartUpStatus.Pending)
                return BadRequest(new { Message = "Chỉ có thể hủy lời mời khi còn ở trạng thái Pending" });

            await _unitOfWork.TeamStartUps.DeleteAsync(teamStartUp);

            return Ok(new { Message = "Đã hủy lời mời" });
        }

        // GET: api/teamstartups/dealing-chats
        // Lấy danh sách các cuộc trao đổi đang Dealing (cho cả chủ startup và người được mời)
        [Authorize(Roles = "Client")]
        [HttpGet("dealing-chats")]
        public async Task<ActionResult<IEnumerable<TeamStartUpDto>>> GetDealingChats()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Không xác định được người dùng" });

            // Lấy các startup của user
            var myStartups = await _unitOfWork.StartUps.GetAllAsync(s => s.UserId == userId);
            var startupIds = myStartups.Select(s => s.Id).ToList();

            // Lấy các lời mời mà user là chủ startup hoặc người được mời, có trạng thái Dealing
            var dealingInvites = await _unitOfWork.TeamStartUps.GetAllAsync(
                t => (startupIds.Contains(t.StartUpId) || t.UserId == userId) && 
                     t.Status == TeamStartUpStatus.Dealing);

            var chatDtos = new List<TeamStartUpDto>();
            
            foreach (var t in dealingInvites)
            {
                var startUp = t.StartUp ?? await _unitOfWork.StartUps.GetByIdAsync(t.StartUpId);
                var owner = startUp != null ? await _unitOfWork.Users.GetByIdAsync(startUp.UserId) : null;
                var user = t.User ?? await _unitOfWork.Users.GetByIdAsync(t.UserId);
                
                chatDtos.Add(new TeamStartUpDto
                {
                    Id = t.Id,
                    StartUpId = t.StartUpId,
                    StartUpIdea = startUp?.Idea ?? "",
                    UserId = t.UserId,
                    UserFullName = user?.FullName ?? "",
                    UserAvatar = user?.Avatar,
                    Status = t.Status,
                    StartupOwnerId = startUp?.UserId ?? "",
                    StartupOwnerName = owner?.FullName ?? "",
                    StartupOwnerAvatar = owner?.Avatar
                });
            }

            return Ok(new { Data = chatDtos, Total = chatDtos.Count });
        }
    }
}
