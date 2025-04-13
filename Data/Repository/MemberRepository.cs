using ease_intro_api.Core.Services;
using ease_intro_api.DTOs.Meet;
using ease_intro_api.DTOs.Member;
using ease_intro_api.Models;
using Microsoft.EntityFrameworkCore;

namespace ease_intro_api.Data.Repository;

public class MemberRepository
{
    private readonly ApplicationDbContext _context;

    public MemberRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    private IQueryable<Member> GetMembersQuery()
    {
        return _context.Member
            .Include(m => m.Meet)
            .ThenInclude(m => m!.Owner)
            .Include(m => m.Meet)
            .ThenInclude(m => m!.Status);
    }

    /// <summary>
    /// Отдать всех участников, без дальнейшего использования ответа для их изменения,
    /// и сохранение этих изменений с базу данных.
    /// </summary>
    /// <returns>Массив с участниками</returns>
    public async Task<List<Member>> GetMembersAsync()
    {
        return await GetMembersQuery()
            .AsNoTracking()
            .ToListAsync();
    }
    
    /// <summary>
    /// Найти, и вернуть участника встречи по идентификатору, если мы точно уверены
    /// что он есть, иначе выкинет исключение.
    /// </summary>
    /// <param name="id">Идентификатор встречи.</param>
    /// <returns>Возвращает найденую встречу.</returns>
    /// <exception cref="InvalidOperationException">Если сработает исключение, пользователь увидет -<br/>
    /// <i>'Status 500, internal server error'</i></exception>>
    public async Task<Member> GetMemberByIdAsync(int id)
    {
        return await GetMembersQuery().FirstAsync(x => x.Id == id);
    }
    
    /// <summary>
    /// Найти, и вернуть участника встречи по идентификатору, если такой имеется.
    /// </summary>
    /// <param name="id">Идентификатор усатника.</param>
    /// <returns>Возвращает найденую данные участника, либо <b>null</b>.</returns>
    public async Task<Member?> GetMemberByIdOrNullAsync(int id)
    {
        return await GetMembersQuery().FirstOrDefaultAsync(x => x.Id == id);
    }
    
    public async Task<Member?> GetMemberByQrCodeOrNullAsync(string qrCode)
    {
        return await GetMembersQuery()
            .FirstOrDefaultAsync(m => m.QrCode == qrCode);
    }

    public async Task<Member> CreateMember(CreateMemberDto dto)
    {
        var member = new Member
        {
            Name = dto.Name,
            Companion = dto.Companion,
            Contact = dto.Contact,
            MeetGuid = dto.MeetUid,
            Role = Member.MemberRole.Guest,
            QrCode = ProcessingQrService.GenerateQr(dto.MeetUid)
        };

        _context.Member.Add(member);
        await _context.SaveChangesAsync();
        
        return member;
    }

    public async Task CreateMemberWithMeet(CreateMeetDto createMeetDto, Meet meet)
    {
        if (createMeetDto.Members != null && createMeetDto.Members.Any())
        {
            var members = createMeetDto.Members.Select(m => new Member
            {
                Name = m.Name,
                Companion = m.Companion,
                Contact = m.Contact,
                Role = m.Role ?? Member.MemberRole.Guest,
                MeetGuid = meet.Uid,
                QrCode = ProcessingQrService.GenerateQr(meet.Uid)
            }).ToList();
                
            _context.Member.AddRange(members);
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateMemberAsync(UpdateMemberDto dto, Member member)
    {
        member.Name = dto.Name;
        member.Companion = dto.Companion;
        member.Contact = dto.Contact;
        member.Role = (Member.MemberRole)dto.Role!.Value;
        
        await _context.SaveChangesAsync();
    }
}