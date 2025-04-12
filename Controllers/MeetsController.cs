using System.Security.Claims;
using ease_intro_api.Core.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ease_intro_api.Data;
using ease_intro_api.DTOs.Meet;
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
    private readonly MemberService _memberService;
    private readonly MeetRepository _meetRepository;
    private readonly MemberRepository _memberRepository;

    public MeetsController
    (
        ApplicationDbContext context,
        ILogger<MeetsController> logger,
        MeetService meetService,
        MeetRepository meetRepository,
        MemberService memberService,
        MemberRepository memberRepository
    )
    {
        _context = context;
        _logger = logger;
        _meetService = meetService;
        _meetRepository = meetRepository;
        _memberService = memberService;
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
    public async Task<ActionResult<IEnumerable<MeetResponseDto>>> GetMeets()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null) { return Unauthorized(); }
        var userId = int.Parse(userIdClaim.Value);
        
        try
        {
            return Ok(await _meetService.ShowAllMeetsAsync(userId));
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
    public async Task<ActionResult<MeetResponseDto>> GetMeet(Guid uid)
    {
        try
        {
            var meet = await _meetRepository.GetMeetByUidOrNullAsync(uid);
            
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
    /// <param name="meetDto">Объект с данными для создания встречи. Может включать массив участников.</param>
    /// <returns>Информация о созданной встрече (MeetResponseDto).</returns>
    [HttpPost]
    [Authorize(Roles = "User")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MeetResponseDto>> CreateMeet([FromBody] MeetCreateDto meetDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null) { return Unauthorized(); }

        var userId = int.Parse(userIdClaim.Value);
        
        if (MeetService.ShiftLimit(meetDto))
        {
            return BadRequest($"Количество участников превышено, допустимо: {meetDto.LimitMembers}.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var meet = await _meetRepository.CreateMeetAsync(meetDto, userId);
            await _memberRepository.CreateMemberWithMeet(meetDto, meet);
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
    /// <param name="meetDto">Объект с новыми данными для обновления встречи.</param>
    /// <returns>Результат обновления. При успешном обновлении возвращается код 204 No Content, если встреча была найдена и обновлена.</returns>
    /// <response code="204">Возвращается, если встреча была успешно обновлена.</response>
    /// <response code="400">Возвращается, если встреча с указанным идентификатором не найдена или данные запроса некорректны.</response>
    /// <response code="404">Возвращается, если встреча была удалена или не существует в базе данных.</response>
    /// <response code="500">Возвращается, если произошла ошибка при обновлении встречи на сервере.</response>
    [HttpPut("{uid:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateMeet([FromRoute] Guid uid, [FromBody] MeetUpdateDto meetDto)
    {

        try
        {
            var meet = await _meetRepository.GetMeetByUidOrNullAsync(uid);
            if (meet == null) { return BadRequest("Встречи с указаным идентификатором не найдено."); }
            
            await _meetRepository.UpdateMeetAsync(meetDto, meet);
            
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteMeet(Guid uid)
    {
        try
        {
            var meet = await _context.Meets.FindAsync(uid);
            if (meet == null) { return NotFound(); }

            _context.Meets.Remove(meet);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении.");
            return StatusCode(500, "Internal server error");
        }
    }
}