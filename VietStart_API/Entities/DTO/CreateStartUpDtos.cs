using VietStart_API.Enums;

namespace VietStart_API.Entities.DTO
{
    public class CreateStartUpDtos
    {
        public string UserId { get; set; }   // thêm vào
        public string Team { get; set; }
        public string Idea { get; set; }
        public string Prototype { get; set; }
        public string Plan { get; set; }
        public string Relationship { get; set; }
        public int Point { get; set; }
        public int IdeaPoint { get; set; }
        public int TeamPoint { get; set; }
        public int PrototypePoint { get; set; }
        public int PlanPoint { get; set; }
        public int RelationshipPoint { get; set; }
        public Privacy Privacy { get; set; }
        public int CategoryId { get; set; }
    }
}
