using ease_intro_api.Data.Repository;
using ease_intro_api.Core.Services.Interfaces;
using ease_intro_api.DTOs.Meet;
using ease_intro_api.DTOs.Member;
using ease_intro_api.Models;
using ease_intro_api.Mappers;

namespace ease_intro_api.Core.Services;

public class MemberService
{
    private readonly MemberRepository _memberRepository;
    private readonly MeetRepository _meetRepository;
    private readonly IUserContextService _userContextService;

    public MemberService
    (
        MemberRepository memberRepository, 
        MeetRepository meetRepository,
        IUserContextService userContextService
    )
    {
        _memberRepository = memberRepository;
        _meetRepository = meetRepository;
        _userContextService = userContextService;
    }
    
    /**
     * Ручная валидация роли у участников, необходима в случае
     * если надо вернуть свою ошибку, а не ту, что предлогает <i>`.net core`</i>
     */
    public static bool CheckRoleMembers(CreateMeetDto createMeetDto)
    {
        return createMeetDto.Members == null || createMeetDto.Members.All(m =>
            !m.Role.HasValue || Enum.IsDefined(typeof(Member.MemberRole), m.Role.Value));
    }

    public async Task<IEnumerable<ResponseMemberDto>> ShowAllMembersAsync()
    {
        var members =  await _memberRepository.GetMembersAsync();
        return members.Select(MemberMapper.MapToDto).ToList();
    }

    public async Task<Member?> ShowMemberByIdOrNullAsync(int id)
    {
        var userId = _userContextService.UserId;
        var member = await _memberRepository.GetMemberByIdOrNullAsync(id);
        if (member == null) { return null; }
        var owner = await _meetRepository.GetMeetByUidAsync(member.MeetGuid);
        
        return owner.OwnerId == userId ? member : null;
    }
}