using ease_intro_api.Core.Services;
using ease_intro_api.Data;
using ease_intro_api.DTOs.Meet;
using ease_intro_api.DTOs.Member;
using ease_intro_api.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ease_intro_api.Core.Repository;

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

    /**
     * Найти, и вернуть участника встречи по идентификатору, если такой имеется.
     * <param name="id">Идентификатор встречи.</param>
     * <returns>Возвращает найденую данные участника, либо <b>null</b>.</returns>
     */
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
            QrCode = ProcessingQr.GenerateQr(dto.MeetUid)
        };

        _context.Member.Add(member);
        await _context.SaveChangesAsync();
        
        return member;
    }

    public async Task CreateMemberWithMeet(MeetCreateDto meetDto, Meet meet)
    {
        if (meetDto.Members != null && meetDto.Members.Any())
        {
            var members = meetDto.Members.Select(m => new Member
            {
                Name = m.Name,
                Companion = m.Companion,
                Contact = m.Contact,
                Role = m.Role ?? Member.MemberRole.Guest,
                MeetGuid = meet.Uid,
                QrCode = ProcessingQr.GenerateQr(meet.Uid)
            }).ToList();
                
            _context.Member.AddRange(members);
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateMember(UpdateMemberDto dto, Member member)
    {
        if (!string.IsNullOrEmpty(dto.Name)) { member.Name = dto.Name; }
        if (!string.IsNullOrEmpty(dto.Companion)) { member.Companion = dto.Companion; }
        if (!string.IsNullOrEmpty(dto.Contact)) { member.Contact = dto.Contact; }
        member.Role = (Member.MemberRole)dto.Role!.Value;
        
        await _context.SaveChangesAsync();
    }
}