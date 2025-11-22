namespace VietStart_API.Entities.DTO
{
    public class RegisterMultiRequestDto
    {
        public List<RegisterRequestDto> Users { get; set; } = new();
    }
}
