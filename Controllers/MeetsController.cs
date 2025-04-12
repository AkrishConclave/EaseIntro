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

    public MeetsController(
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
    
    /**
     * Получить все митинги
     * Также тут пример авторизации
     */
    [HttpGet]
    [Authorize(Roles = "User")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)] // не передан токен
    [ProducesResponseType(StatusCodes.Status403Forbidden)]    // роль не соответствует
    public async Task<ActionResult<IEnumerable<MeetResponseDto>>> GetMeets()
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

    // Получить один митинг по ID
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

    /**
     * <p>Создать новую встречу</p>
     * <p>Сами встречи можно создавать двумя путями просто встречу,
     * и еще можно передать массив с <b>`members`</b>, которые сразу
     * будут записаны на встречу. Так вы сразу можете добавить участников с нужной вам ролью для него.</p>
     * <p>Если вы не верно внесете данные, то отмениться создание встречи, и добавление участников к ней.</p>
     */
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

    // Обновить митинг
    [HttpPut("{uid:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    // Удалить митинг
    [HttpDelete("{uid:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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