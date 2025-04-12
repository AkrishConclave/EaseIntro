using ease_intro_api.Data;
using ease_intro_api.DTOs.Meet;
using ease_intro_api.Models;
using Microsoft.EntityFrameworkCore;

namespace ease_intro_api.Core.Repository;

public class MeetRepository
{
    private readonly ApplicationDbContext _context;

    public MeetRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // Вспомогательный метод для получения данных
    private IQueryable<Meet> GetMeetsQuery()
    {
        return _context.Meets
            .Include(m => m.Status)
            .Include(m => m.Members)
            .Include(m => m.Owner);
    }
    
    /**
     * Получить встречу по идентификатору, если такая имеется.
     * <param name="uid">Идентификатор встречи.</param>
     * <returns>Возвращает найденую встречу, либо <b>null</b>.</returns>
     */
    public async Task<Meet?> GetMeetByUidOrNullAsync(Guid uid)
    {
        return await GetMeetsQuery().FirstOrDefaultAsync(m => m.Uid == uid);
    }
    
    /**
     * Получить встречу по ее идентификатору, если мы точно уверены
     * что она есть, иначе выкинет исключение
     * <param name="uid">Идентификатор встречи.</param>
     * <returns>Возвращает найденую встречу.</returns>
     * <exception cref="InvalidOperationException">Если сработает исключение, пользователь увидет -<br/>
     * <i>'Status 500, internal server error'</i></exception>>
     */
    public async Task<Meet> GetMeetByUidAsync(Guid uid)
    {
        return await GetMeetsQuery().FirstAsync(m => m.Uid == uid);
    }
    
    /**
     * Получить все встречи
     */
    public async Task<List<Meet>> GetAllMeetsAsync(int id)
    {
        return await GetMeetsQuery()
            .Where(m => m.OwnerId == id)
            .ToListAsync();
    }

    public async Task UpdateMeetAsync(MeetUpdateDto meetDto, Meet meet)
    {
        meet.Title = meetDto.Title;
        meet.Date = meetDto.Date;
        meet.Location = meetDto.Location;
        meet.StatusId = meetDto.StatusId;
        meet.LimitMembers = meetDto.LimitMembers;
        meet.AllowedPlusOne = meetDto.AllowedPlusOne;
        
        await _context.SaveChangesAsync();
    }
    
    public async Task<Meet> CreateMeetAsync(MeetCreateDto meetDto, int userId)
    {
        var meet = new Meet
        {
            Uid = Guid.NewGuid(),
            Title = meetDto.Title,
            Date = meetDto.Date,
            Location = meetDto.Location,
            LimitMembers = meetDto.LimitMembers,
            AllowedPlusOne = meetDto.AllowedPlusOne,
            OwnerId = userId,
            StatusId = meetDto.StatusId
        };

        _context.Meets.Add(meet);
        await _context.SaveChangesAsync();
        
        return meet;
    }
}
