namespace VietStart_API.Entities.DTO
{
    public class RoleEmbaddingDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class CreateRoleEmbaddingDto
    {
        public string Name { get; set; }
    }

    public class UpdateRoleEmbaddingDto
    {
        public string Name { get; set; }
    }
}
