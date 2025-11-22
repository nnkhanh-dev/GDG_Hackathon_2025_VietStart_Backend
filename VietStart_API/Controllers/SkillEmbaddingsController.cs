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
    public class SkillEmbaddingsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SkillEmbaddingsController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // GET: api/skillembaddings
        [HttpGet]
        [Authorize(Roles = "Admin,Client")]
        public async Task<ActionResult<IEnumerable<SkillEmbaddingDto>>> GetSkillEmbaddings()
        {
            var skillEmbaddings = await _unitOfWork.SkillEmbadings.GetAllAsync();
            
            var skillEmbaddingDtos = _mapper.Map<IEnumerable<SkillEmbaddingDto>>(skillEmbaddings);

            return Ok(skillEmbaddingDtos);
        }

        // GET: api/skillembaddings/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Client")]
        public async Task<ActionResult<SkillEmbaddingDto>> GetSkillEmbadding(int id)
        {
            var skillEmbadding = await _unitOfWork.SkillEmbadings.GetByIdAsync(id);

            if (skillEmbadding == null)
                return NotFound(new { Message = "Kỹ năng không tồn tại" });

            var skillEmbaddingDto = _mapper.Map<SkillEmbaddingDto>(skillEmbadding);

            return Ok(skillEmbaddingDto);
        }

        // POST: api/skillembaddings
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<SkillEmbaddingDto>> CreateSkillEmbadding([FromBody] CreateSkillEmbaddingDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var skillEmbadding = _mapper.Map<SkillEmbadding>(createDto);

            await _unitOfWork.SkillEmbadings.AddAsync(skillEmbadding);
            await _unitOfWork.SaveChangesAsync();

            var skillEmbaddingDto = _mapper.Map<SkillEmbaddingDto>(skillEmbadding);

            return CreatedAtAction(nameof(GetSkillEmbadding), new { id = skillEmbadding.Id }, skillEmbaddingDto);
        }

        // PUT: api/skillembaddings/{id}
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSkillEmbadding(int id, [FromBody] UpdateSkillEmbaddingDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var skillEmbadding = await _unitOfWork.SkillEmbadings.GetByIdAsync(id);

            if (skillEmbadding == null)
                return NotFound(new { Message = "Kỹ năng không tồn tại" });

            _mapper.Map(updateDto, skillEmbadding);

            await _unitOfWork.SkillEmbadings.UpdateAsync(skillEmbadding);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { Message = "Cập nhật kỹ năng thành công" });
        }

        // DELETE: api/skillembaddings/{id}
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSkillEmbadding(int id)
        {
            var skillEmbadding = await _unitOfWork.SkillEmbadings.GetByIdAsync(id);

            if (skillEmbadding == null)
                return NotFound(new { Message = "Kỹ năng không tồn tại" });

            await _unitOfWork.SkillEmbadings.DeleteAsync(skillEmbadding);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { Message = "Xóa kỹ năng thành công" });
        }
    }
}
