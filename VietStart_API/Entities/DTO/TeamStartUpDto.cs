using VietStart_API.Enums;

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
        public TeamStartUpStatus Status { get; set; }
        
        // Owner information
        public string StartupOwnerId { get; set; }
        public string StartupOwnerName { get; set; }
        public string StartupOwnerAvatar { get; set; }
    }

    public class TeamStartUpDetailDto
    {
        public int Id { get; set; }
        public int StartUpId { get; set; }
        public string UserId { get; set; }
        public TeamStartUpStatus Status { get; set; }

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
    }

    public class UpdateTeamStartUpDto
    {
        public TeamStartUpStatus Status { get; set; }
    }

    public class UpdateJoinRequestStatusDto
    {
        public TeamStartUpStatus Status { get; set; }
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
