using ease_intro_api.DTOs;
using ease_intro_api.DTOs.Meet;
using ease_intro_api.DTOs.Member;
using ease_intro_api.DTOs.User;
using ease_intro_api.Models;

namespace ease_intro_api.Mappers;

public static class MeetMapper
{
    public static ResponseMeetDto MapToDto(Meet meet)
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
            Status = MapStatusToDto(meet.Status!),
            Members = meet.Members?.Select(m => new ResponseMemberDto
            {
                Name = m.Name,
                Companion = m.Companion,
                Contact = m.Contact,
                Role = m.Role.ToString(),
                QrCode = m.QrCode
            })
                .ToList() ?? new List<ResponseMemberDto>()
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