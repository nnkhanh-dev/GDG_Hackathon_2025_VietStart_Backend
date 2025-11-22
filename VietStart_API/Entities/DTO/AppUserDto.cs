namespace VietStart_API.Entities.DTO
{
    public class AppUserDto
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Location { get; set; }
        public string Bio { get; set; }
        public string Avatar { get; set; }
        public DateTime? DOB { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class UpdateAppUserDto
    {
        public string? FullName { get; set; }
        public string? Location { get; set; }
        public string? Bio { get; set; }
        public string? Avatar { get; set; }
        public DateTime? DOB { get; set; }
        public string? Skills { get; set; }
        public string? RolesInStartup { get; set; }
        public string? CategoryInvests { get; set; }
    }

    public class UserSuggestionDto
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Avatar { get; set; }
        public string Bio { get; set; }
        public string Location { get; set; }
        public string Skills { get; set; }
        public string RolesInStartup { get; set; }
        public double MatchScore { get; set; }
        public Dictionary<string, double> MatchDetails { get; set; }
    }

    public class GroupedUserSuggestionDto
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Avatar { get; set; }
        public string Bio { get; set; }
        public string Location { get; set; }
        public string Skills { get; set; }
        public string RolesInStartup { get; set; }
        public double SkillMatchScore { get; set; }
        public double RoleMatchScore { get; set; }
        public double CategoryMatchScore { get; set; }
        public double OverallScore { get; set; }
    }

    public class GroupedSuggestionsResponseDto
    {
        public List<GroupedUserSuggestionDto> BySkills { get; set; } = new();
        public List<GroupedUserSuggestionDto> ByRoles { get; set; } = new();
        public List<GroupedUserSuggestionDto> ByCategory { get; set; } = new();
        public List<GroupedUserSuggestionDto> Overall { get; set; } = new();
    }
}
