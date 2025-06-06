using ease_intro_api.DTOs;
using ease_intro_api.DTOs.Meet;
using ease_intro_api.DTOs.Member;
using ease_intro_api.DTOs.User;
using ease_intro_api.Models;

namespace ease_intro_api.Mappers;

public static class MemberMapper
{
    public static ResponseMemberDto MapToDto(Member member)
    {
        return new ResponseMemberDto
        {
            Name = member.Name,
            Companion = member.Companion,
            Contact = member.Contact,
            Role = member.Role.ToString(),
            QrCode = member.QrCode,
            Meet = MapMeetToDto(member.Meet!)
        };
    }

    private static ResponseMeetDto MapMeetToDto(Meet meet)
    {
        return new ResponseMeetDto
        {
            Uid = meet.Uid,
            Title = meet.Title,
            Date = meet.Date,
            Location = meet.Location,
            LimitMembers = meet.LimitMembers,
            AllowedPlusOne = meet.AllowedPlusOne,
            Owner = MapUserToDto(meet.Owner),
            Status = MapStatusToDto(meet.Status!)
        };
    }

    private static ResponseUserDto MapUserToDto(User user)
    {
        return new ResponseUserDto
        {
            PublicName = user.PublicName,
            PublicContact = user.PublicContact
        };
    }

    private static MeetStatusDto MapStatusToDto(MeetStatus status)
    {
        return new MeetStatusDto
        {
            Title = status.Title,
            Description = status.Description
        };
    }
}