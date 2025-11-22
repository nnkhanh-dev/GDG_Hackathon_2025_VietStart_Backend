using VietStart_API.Enums;

namespace VietStart_API.Entities.DTO
{
    public class ReactDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string UserFullName { get; set; }
        public int? CommentId { get; set; }
        public int? StartUpId { get; set; }
        public ReactType Type { get; set; }
    }

    public class CreateReactDto
    {
        public int? CommentId { get; set; }
        public int? StartUpId { get; set; }
        public ReactType Type { get; set; }
    }

    public class UpdateReactDto
    {
        public ReactType Type { get; set; }
    }
}
