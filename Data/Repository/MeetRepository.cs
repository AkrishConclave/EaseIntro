using ease_intro_api.Core.Services.Interfaces;
using ease_intro_api.DTOs.Meet;
using ease_intro_api.Models;
using Microsoft.EntityFrameworkCore;

namespace ease_intro_api.Data.Repository;

public class MeetRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IUserContextService _userContextService;

    public MeetRepository
    (
        ApplicationDbContext context,
        IUserContextService userContextService
    )
    {
        _context = context;
        _userContextService = userContextService;
    }

    /// <summary>
    /// Вспомогательный метод для получения данных
    /// </summary>
    /// <returns>Данные со связями.</returns>
    private IQueryable<Meet> GetMeetsQuery()
    {
        return _context.Meets
            .Include(m => m.Status)
            .Include(m => m.Members)
            .Include(m => m.Owner);
    }
    
    /// <summary>
    /// Получить встречу по идентификатору, если такая имеется.
    /// </summary>
    /// <param name="uid">Идентификатор встречи.</param>
    /// <returns>Возвращает найденую встречу, либо <b>null</b>.</returns>
    public async Task<Meet?> GetMeetByUidOrNullAsync(Guid uid)
    {
        var userId = _userContextService.UserId;
        return await GetMeetsQuery()
            .Where(m => m.OwnerId == userId)
            .FirstOrDefaultAsync(m => m.Uid == uid);
    }
    
    /// <summary>
    /// Получить встречу по идентификатору, если такая имеется. Публичный метод, не для обновления или создания
    /// </summary>
    /// <param name="uid">Идентификатор встречи.</param>
    /// <returns>Возвращает найденую встречу, либо <b>null</b>.</returns>
    public async Task<Meet?> PublicGetMeetByUidOrNullAsync(Guid uid)
    {
        return await GetMeetsQuery().FirstOrDefaultAsync(m => m.Uid == uid);
    }
    
    /// <summary>
    /// Получить встречу по ее идентификатору, если мы точно уверены что она есть, иначе выкинет исключение/
    /// </summary>
    /// <param name="uid">Идентификатор встречи.</param>
    /// <returns>Возвращает найденую встречу.</returns>
    /// <exception cref="InvalidOperationException">Если сработает исключение, пользователь увидет -<br/>
    /// <i>'Status 500, internal server error'</i></exception>>
    public async Task<Meet> GetMeetByUidAsync(Guid uid)
    {
        return await GetMeetsQuery().FirstAsync(m => m.Uid == uid);
    }
    
    /// <summary>
    /// Получаем все встречи авторизированного пользователя.
    /// </summary>
    /// <param name="id">Идентификатор авторизированного пользователя</param>
    /// <returns>Массив со всеми встречамию.</returns>
    public async Task<List<Meet>> GetAllMeetsAsync(int id)
    {
        return await GetMeetsQuery()
            .Where(m => m.OwnerId == id)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task UpdateMeetAsync(UpdateMeetDto updateMeetDto, Meet meet)
    {
        meet.Title = updateMeetDto.Title;
        meet.Date = updateMeetDto.Date;
        meet.Location = updateMeetDto.Location;
        meet.StatusId = updateMeetDto.StatusId;
        meet.LimitMembers = updateMeetDto.LimitMembers;
        meet.AllowedPlusOne = updateMeetDto.AllowedPlusOne;
        
        await _context.SaveChangesAsync();
    }
    
    public async Task<Meet> CreateMeetAsync(CreateMeetDto createMeetDto)
    {
        var userId = _userContextService.UserId;
        
        var meet = new Meet
        {
            Uid = Guid.NewGuid(),
            Title = createMeetDto.Title,
            Date = createMeetDto.Date,
            Location = createMeetDto.Location,
            LimitMembers = createMeetDto.LimitMembers,
            AllowedPlusOne = createMeetDto.AllowedPlusOne,
            OwnerId = userId,
            StatusId = createMeetDto.StatusId
        };

        _context.Meets.Add(meet);
        await _context.SaveChangesAsync();
        
        return meet;
    }
}
