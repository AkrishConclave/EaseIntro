using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ease_intro_api.Data;
using ease_intro_api.DTOs;
using ease_intro_api.Models;
using ease_intro_api.DTOs.Meet;
using ease_intro_api.DTOs.Member;
using ease_intro_api.DTOs.User;
using Microsoft.AspNetCore.Authorization;

namespace ease_intro_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MeetsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MeetsController> _logger;

    public MeetsController(ApplicationDbContext context, ILogger<MeetsController> logger)
    {
        _context = context;
        _logger = logger;
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
            var meets = await _context.Meets
                .Include(m => m.Status)
                .Include(m => m.Members) // Загружаем участников
                .Include(m => m.Owner)
                .Select(m => new MeetResponseDto
                {
                    Uid = m.Uid,
                    Title = m.Title,
                    Date = m.Date,
                    Location = m.Location,
                    LimitMembers = m.LimitMembers,
                    AllowedPlusOne = m.AllowedPlusOne,
                    Owner = new UserResponseDto
                    {
                        PublicName = m.Owner.PublicName,
                        PublicContact = m.Owner.PublicContact,
                    },
                    Status = new MeetStatusDto
                    {
                        Title = m.Status!.Title,
                        Description = m.Status.Description
                    },
                    Members = m.Members.Select(member => new MemberResponseDto
                    {
                        Name = member.Name,
                        Companion = member.Companion,
                        Contact = member.Contact,
                        Role = member.Role.ToString(),
                        QrCode = member.QrCode
                    }).ToList()
                })
                .ToListAsync();

            return Ok(meets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all meets");
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
            var meet = await _context.Meets
                .Include(m => m.Status)
                .Include(m => m.Members)
                .Include(m => m.Owner)
                .FirstOrDefaultAsync(m => m.Uid == uid);

            if (meet == null)
                return NotFound();

            return Ok(new MeetResponseDto
            {
                Uid = meet.Uid,
                Title = meet.Title,
                Date = meet.Date,
                Location = meet.Location,
                LimitMembers = meet.LimitMembers,
                AllowedPlusOne = meet.AllowedPlusOne,
                Owner = new UserResponseDto
                {
                    PublicName = meet.Owner.PublicName,
                    PublicContact = meet.Owner.PublicContact,
                },
                Status = new MeetStatusDto
                {
                    Title = meet.Status!.Title,
                    Description = meet.Status.Description
                },
                Members = meet.Members.Select(member => new MemberResponseDto
                {
                    Name = member.Name,
                    Companion = member.Companion,
                    Contact = member.Contact,
                    Role = member.Role.ToString(),
                    QrCode = member.QrCode
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting meet with ID: {uid}");
            return StatusCode(500, "Internal server error");
        }
    }

    /**
     * <p>Создать новую встречу</p>
     * <p>Сами встречи можно создавать двумя путями просто встречу,
     * и еще можно передать массив с <b>`members`</b>, которые сразу
     * будут записаны на встречу.</p>
     */
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MeetResponseDto>> CreateMeet([FromBody] MeetCreateDto meetDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized();

        var userId = int.Parse(userIdClaim.Value);

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var statusExists = await _context.MeetStatus.AnyAsync(s => s.Id == meetDto.StatusId);
            if (!statusExists)
                return BadRequest("Invalid StatusId");

            // Создаем встречу
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

            // Добавляем участников, если они есть
            if (meetDto.Members != null && meetDto.Members.Any())
            {
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                
                var members = meetDto.Members.Select(m => new Member
                {
                    Name = m.Name,
                    Companion = m.Companion,
                    Contact = m.Contact,
                    Role = m.Role,
                    MeetGuid = meet.Uid,
                    QrCode = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{timestamp}-{meet.Uid}"))
                }).ToList();

                _context.Member.AddRange(members);
                await _context.SaveChangesAsync();
            }

            // Загружаем созданную встречу с участниками
            var createdMeet = await _context.Meets
                .Include(m => m.Status)
                .Include(m => m.Members)
                .Include(m => m.Owner)
                .FirstAsync(m => m.Uid == meet.Uid);

            return CreatedAtAction(nameof(GetMeet), 
                new { uid = createdMeet.Uid },
                new MeetResponseDto
                {
                    Uid = createdMeet.Uid,
                    Title = createdMeet.Title,
                    Date = createdMeet.Date,
                    Location = createdMeet.Location,
                    LimitMembers = createdMeet.LimitMembers,
                    AllowedPlusOne = createdMeet.AllowedPlusOne,
                    Owner = new UserResponseDto
                    {
                        PublicName = createdMeet.Owner.PublicName,
                        PublicContact = createdMeet.Owner.PublicContact,
                    },
                    Status = new MeetStatusDto
                    {
                        Title = createdMeet.Status!.Title,
                        Description = createdMeet.Status.Description
                    },
                    Members = createdMeet.Members.Select(member => new MemberResponseDto
                    {
                        Name = member.Name,
                        Companion = member.Companion,
                        Contact = member.Contact,
                        Role = member.Role.ToString(),
                        QrCode = member.QrCode
                    }).ToList()
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating meet");
            return StatusCode(500, "Internal server error");
        }
    }


    // Обновить митинг
    [HttpPut("{uid:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMeet(
        [FromRoute] Guid uid,
        [FromBody] MeetUpdateDto meetDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var meet = await _context.Meets.FindAsync(uid);
            if (meet == null)
                return NotFound();

            var statusExists = await _context.MeetStatus.AnyAsync(s => s.Id == meetDto.StatusId);
            if (!statusExists)
                return BadRequest("Invalid StatusId");

            meet.Title = meetDto.Title;
            meet.Date = meetDto.Date;
            meet.Location = meetDto.Location;
            meet.StatusId = meetDto.StatusId;

            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, $"Concurrency error updating meet with ID: {uid}");
            if (!_context.Meets.Any(m => m.Uid == uid))
                return NotFound();
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating meet with ID: {uid}");
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
            if (meet == null)
                return NotFound();

            _context.Meets.Remove(meet);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting meet with ID: {uid}");
            return StatusCode(500, "Internal server error");
        }
    }
}