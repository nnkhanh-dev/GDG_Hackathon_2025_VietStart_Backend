namespace VietStart_API.Entities.DTO
{
    public class SkillEmbaddingDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class CreateSkillEmbaddingDto
    {
        public string Name { get; set; }
    }

    public class UpdateSkillEmbaddingDto
    {
        public string Name { get; set; }
    }
}
