namespace VietStart_API.Entities.DTO
{
    public class TeamStartUpDto
    {
        public int Id { get; set; }
        public int StartUpId { get; set; }
        public string StartUpIdea { get; set; }
        public string UserId { get; set; }
        public string UserFullName { get; set; }
        public string UserAvatar { get; set; }
        public string Status { get; set; }
    }

    public class TeamStartUpDetailDto
    {
        public int Id { get; set; }
        public int StartUpId { get; set; }
        public string UserId { get; set; }
        public string Status { get; set; }

        // Related entities
        public AppUserDto User { get; set; }
        public StartUpDto StartUp { get; set; }
    }

    public class SendJoinRequestDto
    {
        public int StartUpId { get; set; }
    }

    public class CreateTeamStartUpDto
    {
        public int StartUpId { get; set; }
        public string UserId { get; set; }
        public string Status { get; set; } = "Pending"; // Default status
    }

    public class UpdateTeamStartUpDto
    {
        public string Status { get; set; }
    }

    public class UpdateJoinRequestStatusDto
    {
        public string Status { get; set; } // "Accepted" or "Rejected"
        public string? Reason { get; set; }
    }

    public class PositionDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class CreatePositionDto
    {
        public string Name { get; set; }
    }

    public class UpdatePositionDto
    {
        public string Name { get; set; }
    }
}
