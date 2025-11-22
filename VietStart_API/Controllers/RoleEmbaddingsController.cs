using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VietStart_API.Entities.Domains;
using VietStart_API.Entities.DTO;
using VietStart_API.Repositories;

namespace VietStart.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleEmbaddingsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public RoleEmbaddingsController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // GET: api/roleembaddings
        [HttpGet]
        [Authorize(Roles = "Admin,Client")]
        public async Task<ActionResult<IEnumerable<RoleEmbaddingDto>>> GetRoleEmbaddings()
        {
            var roleEmbaddings = await _unitOfWork.RoleEmbadings.GetAllAsync();
            
            var roleEmbaddingDtos = _mapper.Map<IEnumerable<RoleEmbaddingDto>>(roleEmbaddings);

            return Ok(roleEmbaddingDtos);
        }

        // GET: api/roleembaddings/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Client")]
        public async Task<ActionResult<RoleEmbaddingDto>> GetRoleEmbadding(int id)
        {
            var roleEmbadding = await _unitOfWork.RoleEmbadings.GetByIdAsync(id);

            if (roleEmbadding == null)
                return NotFound(new { Message = "Vai trò không tồn tại" });

            var roleEmbaddingDto = _mapper.Map<RoleEmbaddingDto>(roleEmbadding);

            return Ok(roleEmbaddingDto);
        }

        // POST: api/roleembaddings
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<RoleEmbaddingDto>> CreateRoleEmbadding([FromBody] CreateRoleEmbaddingDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var roleEmbadding = _mapper.Map<RoleEmbadding>(createDto);

            await _unitOfWork.RoleEmbadings.AddAsync(roleEmbadding);
            await _unitOfWork.SaveChangesAsync();

            var roleEmbaddingDto = _mapper.Map<RoleEmbaddingDto>(roleEmbadding);

            return CreatedAtAction(nameof(GetRoleEmbadding), new { id = roleEmbadding.Id }, roleEmbaddingDto);
        }

        // PUT: api/roleembaddings/{id}
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRoleEmbadding(int id, [FromBody] UpdateRoleEmbaddingDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var roleEmbadding = await _unitOfWork.RoleEmbadings.GetByIdAsync(id);

            if (roleEmbadding == null)
                return NotFound(new { Message = "Vai trò không tồn tại" });

            _mapper.Map(updateDto, roleEmbadding);

            await _unitOfWork.RoleEmbadings.UpdateAsync(roleEmbadding);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { Message = "Cập nhật vai trò thành công" });
        }

        // DELETE: api/roleembaddings/{id}
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoleEmbadding(int id)
        {
            var roleEmbadding = await _unitOfWork.RoleEmbadings.GetByIdAsync(id);

            if (roleEmbadding == null)
                return NotFound(new { Message = "Vai trò không tồn tại" });

            await _unitOfWork.RoleEmbadings.DeleteAsync(roleEmbadding);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { Message = "Xóa vai trò thành công" });
        }
    }
}
