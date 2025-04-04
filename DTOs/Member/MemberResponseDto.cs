using ease_intro_api.DTOs.Meet;

namespace ease_intro_api.DTOs.Member;

public class MemberResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Companion { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public MeetResponseDto? Meet { get; set; }
}