using ease_intro_api.DTOs.Member;
using ease_intro_api.DTOs.User;

namespace ease_intro_api.DTOs.Meet;

public class ResponseMeetDto
{
    public Guid Uid { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Location { get; set; } = string.Empty;
    public MeetStatusDto? Status { get; set; }
    public int LimitMembers { get; set; }
    public bool AllowedPlusOne { get; set; }
    public List<ResponseMemberDto> Members { get; set; } = new();
    public ResponseUserDto Owner { get; set; } = new();

}