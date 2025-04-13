using ease_intro_api.Data.Repository;
using ease_intro_api.Core.Services.Interfaces;
using ease_intro_api.DTOs.Meet;
using ease_intro_api.Mappers;

namespace ease_intro_api.Core.Services;

public class MeetService
{
    private readonly MeetRepository _meetRepository;
    private readonly IUserContextService _userContextService;

    public MeetService
    (
        MeetRepository meetRepository,
        IUserContextService userContextService
    )
    {
        _meetRepository = meetRepository;
        _userContextService = userContextService;
    }
    
    public static bool ShiftLimit(CreateMeetDto createMeetDto)
    {
        var count = createMeetDto.Members!.Count;
        return createMeetDto.LimitMembers != 0 && count > createMeetDto.LimitMembers;
    }
    
    public async Task<IEnumerable<ResponseMeetDto>> ShowAllMeetsAsync()
    {
        var userId = _userContextService.UserId;
        var meets = await _meetRepository.GetAllMeetsAsync(userId);
        return meets.Select(MeetMapper.MapToDto).ToList();
    }
}