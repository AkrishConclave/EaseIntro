using ease_intro_api.Core.Repository;
using ease_intro_api.DTOs.Meet;
using ease_intro_api.Mappers;

namespace ease_intro_api.Core.Services;

public class MeetService
{
    private readonly MeetRepository _meetRepository;

    public MeetService(MeetRepository meetRepository)
    {
        _meetRepository = meetRepository;
    }
    
    public static bool ShiftLimit(MeetCreateDto meetDto)
    {
        var count = meetDto.Members!.Count;
        return meetDto.LimitMembers != 0 && count > meetDto.LimitMembers;
    }
    
    public async Task<IEnumerable<MeetResponseDto>> ShowAllMeetsAsync()
    {
        var meets = await _meetRepository.GetAllMeetsAsync();
        return meets.Select(MeetMapper.MapToDto).ToList();
    }
}