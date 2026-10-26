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
    /// <i>'Status 500, internal server error'</i></exception>
    public async Task<Member> GetMemberByIdAsync(int id)
    {
        return await GetMembersQuery().FirstAsync(x => x.Id == id);
    }
    
    /// <summary>
    /// Найти, и вернуть участника встречи по контакту, если мы точно уверены
    /// что он есть, иначе выкинет исключение.
    /// </summary>
    /// <param name="contact">Контакт учатника.</param>
    /// <returns>Возвращает найденого участника.</returns>
    /// <exception cref="InvalidOperationException">Если сработает исключение, пользователь увидет -<br/>
    /// <i>'Status 500, internal server error'</i></exception>
    public async Task<List<Member>> GetMemberByContactAsync(string contact, int ownerId)
    {
        return await GetMembersQuery()
            .Where(x => x.Contact == contact && x.Meet!.OwnerId == ownerId)
            .ToListAsync();
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
            Companion = dto.Companion ?? string.Empty,
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
                Companion = m.Companion ?? string.Empty,
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
        if (!string.IsNullOrWhiteSpace(dto.Name)) member.Name = dto.Name;
        if (dto.Companion != null) member.Companion = dto.Companion;
        if (!string.IsNullOrWhiteSpace(dto.Contact)) member.Contact = dto.Contact;
        if (dto.Role.HasValue) member.Role = (Member.MemberRole)dto.Role.Value;
        
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Быстрый метод проверки на существование записи
    /// </summary>
    /// <param name="contact">Email пользователя</param>
    /// <param name="meetUid">Номер встречи</param>
    /// <returns>bool</returns>
    public async Task<bool> CheckExistsContactAsync(string contact, Guid meetUid)
    {
        return await _context.Member.AnyAsync(x => x.Contact == contact && x.MeetGuid == meetUid);
    }

    /// <summary>
    /// Отметить участника как пришедшего
    /// </summary>
    /// <param name="member">Участник для отметки</param>
    /// <returns>Обновленный участник</returns>
    public async Task<bool> CheckInMemberAsync(Member member)
    {
        var now = DateTime.UtcNow;
        if (_context.Database.IsRelational())
        {
            var changed = await _context.Member
                .Where(m => m.Id == member.Id && !m.IsCheckedIn)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(m => m.IsCheckedIn, true)
                    .SetProperty(m => m.CheckedInAt, (DateTime?)now));
            await _context.Entry(member).ReloadAsync();
            return changed == 1;
        }
        if (member.IsCheckedIn) return false;
        member.IsCheckedIn = true;
        member.CheckedInAt = now;
        await _context.SaveChangesAsync();
        return true;
    }
}
