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

    public static PublicResponseMeetDto MapToPublicDto(Meet meet)
    {
        var currentMembersCount = meet.Members?.Count ?? 0;
        int? availableSpots = meet.LimitMembers > 0 ? Math.Max(0, meet.LimitMembers - currentMembersCount) : null;

        return new PublicResponseMeetDto
        {
            Uid = meet.Uid,
            Title = meet.Title,
            Date = meet.Date,
            Location = meet.Location,
            LimitMembers = meet.LimitMembers,
            AllowedPlusOne = meet.AllowedPlusOne,
            Owner = MapUserToDto(meet.Owner),
            Status = meet.Status != null ? MapStatusToDto(meet.Status) : null,
            CurrentMembersCount = currentMembersCount,
            AvailableSpots = availableSpots
        };
    }

    private static ResponseUserDto MapUserToDto(User user)
    {
        if (user == null) return new ResponseUserDto();
        return new ResponseUserDto
        {
            PublicName = user.PublicName,
            PublicContact = user.PublicContact
        };
    }

    private static MeetStatusDto MapStatusToDto(MeetStatus status)
    {
        if (status == null) return new MeetStatusDto();
        return new MeetStatusDto
        {
            Title = status.Title,
            Description = status.Description
        };
    }
}
