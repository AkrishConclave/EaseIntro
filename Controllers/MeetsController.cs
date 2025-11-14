using System.Security.Claims;
using ease_intro_api.Data.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ease_intro_api.Data;
using ease_intro_api.DTOs.Meet;
using ease_intro_api.DTOs.Member;
using Microsoft.AspNetCore.Authorization;
using ease_intro_api.Core.Services;
using ease_intro_api.Mappers;

namespace ease_intro_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MeetsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MeetsController> _logger;
    private readonly MeetService _meetService;
    private readonly MeetRepository _meetRepository;
    private readonly MemberRepository _memberRepository;

    public MeetsController
    (
        ApplicationDbContext context,
        ILogger<MeetsController> logger,
        MeetService meetService,
        MeetRepository meetRepository,
        MemberRepository memberRepository
    )
    {
        _context = context;
        _logger = logger;
        _meetService = meetService;
        _meetRepository = meetRepository;
        _memberRepository = memberRepository;
    }
    
    /// <summary>
    /// Получить все митинги для пользователя
    /// </summary>
    /// <remarks>
    /// Этот метод возвращает список всех встреч для авторизованного пользователя. Пользователь должен быть авторизован
    /// и иметь роль "User" для доступа к этому методу. В ответе возвращается список встреч с информацией о каждой встрече.
    /// </remarks>
    /// <returns>
    /// Возвращает список встреч, если пользователь авторизован. В случае ошибок в процессе запроса возвращает соответствующие коды состояния.
    /// </returns>
    /// Возвращает список встреч (MeetResponseDto) для авторизованного пользователя (200 OK).
    /// Возвращает 401 Unauthorized, если пользователь не авторизован.
    /// Возвращает 403 Forbidden, если у пользователя нет прав для выполнения этого действия.
    /// Возвращает 500 Internal Server Error, в случае исключения во время обработки запроса.
    [HttpGet]
    [Authorize(Roles = "User")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ResponseMeetDto>>> GetMeets()
    {
        try
        {
            return Ok(await _meetService.ShowAllMeetsAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения встреч");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Получить информацию о митинге по его уникальному идентификатору (UID).
    /// </summary>
    /// <remarks>
    /// Этот метод возвращает информацию о митинге по его уникальному идентификатору в формате DTO. 
    /// В случае, если митинг с указанным идентификатором не найден, возвращается ошибка 404 NotFound.
    /// </remarks>
    /// <param name="uid">Уникальный идентификатор митинга (GUID).</param>
    /// <returns>Возвращает информацию о митинге (MeetResponseDto) или ошибку 404, если митинг не найден.</returns>
    [HttpGet("{uid:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseMeetDto>> GetMeet(Guid uid)
    {
        try
        {
            var meet = await _meetRepository.PublicGetMeetByUidOrNullAsync(uid);
            
            if (meet == null) { return BadRequest("Встречи с указаным идентификатором не найдено."); }
            return Ok(MeetMapper.MapToDto(meet));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения встречи по ее индетификатору");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Создание новой встречи.
    /// </summary>
    /// <remarks>
    /// Этот метод позволяет создать новую встречу. Если при создании встречи передается массив участников (members),
    /// они автоматически добавляются к встрече с соответствующими ролями.
    /// В случае неверных данных (например, превышение лимита участников), создание встречи будет отменено.
    /// </remarks>
    /// <param name="createMeetDto">Объект с данными для создания встречи. Может включать массив участников.</param>
    /// <returns>Информация о созданной встрече (MeetResponseDto).</returns>
    [HttpPost]
    [Authorize(Roles = "User")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResponseMeetDto>> CreateMeet([FromBody] CreateMeetDto createMeetDto)
    {
        // Валидация даты - нельзя создавать встречи в прошлом
        if (createMeetDto.Date < DateTime.UtcNow)
        {
            return BadRequest("Нельзя создать встречу с датой в прошлом.");
        }
        
        if (MeetService.ShiftLimit(createMeetDto))
        {
            return BadRequest($"Количество участников превышено, допустимо: {createMeetDto.LimitMembers}.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var meet = await _meetRepository.CreateMeetAsync(createMeetDto);
            await _memberRepository.CreateMemberWithMeet(createMeetDto, meet);
            await transaction.CommitAsync();
            var createdMeet = await _meetRepository.GetMeetByUidAsync(meet.Uid);

            return CreatedAtAction(nameof(GetMeet), 
                new { uid = createdMeet.Uid },
                MeetMapper.MapToDto(createdMeet));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Ошибка при создании встречи");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Обновление информации о встрече.
    /// </summary>
    /// <remarks>
    /// Этот метод обновляет данные о встрече по ее уникальному идентификатору (UID). 
    /// Если встреча с таким UID не найдена, возвращается ошибка. Также предусмотрена обработка ошибок конкурентных обновлений.
    /// </remarks>
    /// <param name="uid">Уникальный идентификатор встречи (GUID).</param>
    /// <param name="updateMeetDto">Объект с новыми данными для обновления встречи.</param>
    /// <returns>Результат обновления. При успешном обновлении возвращается код 204 No Content, если встреча была найдена и обновлена.</returns>
    /// <response code="204">Возвращается, если встреча была успешно обновлена.</response>
    /// <response code="400">Возвращается, если встреча с указанным идентификатором не найдена или данные запроса некорректны.</response>
    /// <response code="404">Возвращается, если встреча была удалена или не существует в базе данных.</response>
    /// <response code="500">Возвращается, если произошла ошибка при обновлении встречи на сервере.</response>
    [HttpPut("{uid:guid}")]
    [Authorize(Roles = "User")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateMeet([FromRoute] Guid uid, [FromBody] UpdateMeetDto updateMeetDto)
    {
        try
        {
            var meet = await _meetRepository.GetMeetByUidOrNullAsync(uid);
            if (meet == null) { return NotFound("Встречи с указаным идентификатором не найдено."); }
            
            // Проверка владельца встречи
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Forbid();

            if (!int.TryParse(userIdClaim, out var userId))
                return StatusCode(500, "Некорректный токен (userId не int).");

            if (meet.OwnerId != userId)
                return Forbid();
            
            // Валидация даты - нельзя создавать встречи в прошлом
            if (updateMeetDto.Date < DateTime.UtcNow)
            {
                return BadRequest("Нельзя установить дату встречи в прошлом.");
            }
            
            await _meetRepository.UpdateMeetAsync(updateMeetDto, meet);
            
            return NoContent();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Другой пользователь уже изменил встречу, либо ее могли удалить.");
            if (!_context.Meets.Any(m => m.Uid == uid)) { return NotFound(); }
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обновления встречи с указаным идентификатором.");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Получить список участников встречи.
    /// </summary>
    /// <remarks>
    /// Этот метод возвращает список всех участников конкретной встречи. Доступен только владельцу встречи.
    /// </remarks>
    /// <param name="uid">Уникальный идентификатор встречи (GUID).</param>
    /// <returns>Список участников встречи.</returns>
    [HttpGet("{uid:guid}/members")]
    [Authorize(Roles = "User")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ResponseMemberDto>>> GetMeetMembers(Guid uid)
    {
        try
        {
            var meet = await _meetRepository.GetMeetByUidOrNullAsync(uid);
            if (meet == null)
                return NotFound("Встреча с указанным идентификатором не найдена.");
            
            // Проверка владельца
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Forbid();

            if (!int.TryParse(userIdClaim, out var userId))
                return StatusCode(500, "Некорректный токен (userId не int).");

            if (meet.OwnerId != userId)
                return Forbid();
            
            var members = meet.Members.Select(MemberMapper.MapToDto).ToList();
            return Ok(members);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения участников встречи");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Получить статистику посещаемости встречи.
    /// </summary>
    /// <remarks>
    /// Возвращает список участников, которые пришли на мероприятие, и время их сканирования.
    /// Доступно только владельцу встречи.
    /// </remarks>
    /// <param name="uid">Уникальный идентификатор встречи (GUID).</param>
    /// <returns>Статистика посещаемости: кто пришел и во сколько.</returns>
    [HttpGet("{uid:guid}/attendance")]
    [Authorize(Roles = "User")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetAttendanceStats(Guid uid)
    {
        try
        {
            var meet = await _meetRepository.GetMeetByUidOrNullAsync(uid);
            if (meet == null)
                return NotFound("Встреча с указанным идентификатором не найдена.");
            
            // Проверка владельца
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Forbid();

            if (!int.TryParse(userIdClaim, out var userId))
                return StatusCode(500, "Некорректный токен (userId не int).");

            if (meet.OwnerId != userId)
                return Forbid();
            
            var checkedInMembers = meet.Members
                .Where(m => m.IsCheckedIn)
                .OrderBy(m => m.CheckedInAt)
                .Select(m => new
                {
                    name = m.Name,
                    checkedInAt = m.CheckedInAt!.Value
                })
                .ToList();
            
            return Ok(new
            {
                meetTitle = meet.Title,
                meetDate = meet.Date,
                totalCheckedIn = checkedInMembers.Count,
                members = checkedInMembers
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения статистики посещаемости");
            return StatusCode(500, "Internal server error");
        }
    }
    
    /// <summary>
    /// Выгрузить статистику посещаемости в CSV формате.
    /// </summary>
    /// <remarks>
    /// Выгружает статистику посещаемости в CSV файл с полями: имя, дата (время сканирования).
    /// Доступно только владельцу встречи.
    /// </remarks>
    /// <param name="uid">Уникальный идентификатор встречи (GUID).</param>
    /// <returns>CSV файл со статистикой посещаемости.</returns>
    [HttpGet("{uid:guid}/attendance/export")]
    [Authorize(Roles = "User")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> ExportAttendanceStats(Guid uid)
    {
        try
        {
            var meet = await _meetRepository.GetMeetByUidOrNullAsync(uid);
            if (meet == null)
                return NotFound("Встреча с указанным идентификатором не найдена.");
            
            // Проверка владельца
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Forbid();

            if (!int.TryParse(userIdClaim, out var userId))
                return StatusCode(500, "Некорректный токен (userId не int).");

            if (meet.OwnerId != userId)
                return Forbid();
            
            var checkedInMembers = meet.Members
                .Where(m => m.IsCheckedIn)
                .OrderBy(m => m.CheckedInAt)
                .ToList();
            
            // Формируем CSV
            var csvLines = new List<string>
            {
                "Имя,Дата и время сканирования"
            };
            
            foreach (var member in checkedInMembers)
            {
                var dateStr = member.CheckedInAt!.Value.ToString("yyyy-MM-dd HH:mm:ss");
                csvLines.Add($"\"{member.Name}\",\"{dateStr}\"");
            }
            
            var csvContent = string.Join("\n", csvLines);
            var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
            
            var fileName = $"attendance_{meet.Title.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd}.csv";
            
            return File(bytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка выгрузки статистики посещаемости");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Удаление встречи по уникальному идентификатору.
    /// </summary>
    /// <remarks>
    /// Этот метод удаляет встречу по ее уникальному идентификатору (UID). 
    /// Если встреча с указанным идентификатором не найдена, возвращается ошибка.
    /// </remarks>
    /// <param name="uid">Уникальный идентификатор встречи (GUID).</param>
    /// <returns>Результат удаления. При успешном удалении возвращается код 204 No Content.</returns>
    /// <response code="204">Возвращается, если встреча была успешно удалена.</response>
    /// <response code="404">Возвращается, если встреча с указанным идентификатором не найдена.</response>
    /// <response code="500">Возвращается, если произошла ошибка при удалении встречи на сервере.</response>
    [HttpDelete("{uid:guid}")]
    [Authorize(Roles = "User")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteMeet(Guid uid)
    {
        try
        {
            var meet = await _context.Meets.FindAsync(uid);
            if (meet == null)
                return NotFound();

            // Берём ID текущего пользователя из токена
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Forbid();

            if (!int.TryParse(userIdClaim, out var userId))
                return StatusCode(500, "Некорректный токен (userId не int).");

            // Сравниваем владельца
            if (meet.OwnerId != userId)
                return Forbid();

            _context.Meets.Remove(meet);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении встречи.");
            return StatusCode(500, "Internal server error");
        }
    }
}