using ease_intro_api.DTOs.Meet;

namespace ease_intro_api.DTOs.Member;

public class ResponseMemberDto
{
    public string Name { get; set; } = string.Empty;
    public string Companion { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string QrCode { get; set; } = string.Empty;
    public bool IsCheckedIn { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public ResponseMeetDto? Meet { get; set; }
}