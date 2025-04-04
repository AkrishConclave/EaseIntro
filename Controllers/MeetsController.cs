using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ease_intro_api.Data;
using ease_intro_api.DTOs;
using ease_intro_api.Models;
using ease_intro_api.DTOs.Meet;
using ease_intro_api.DTOs.Member;

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

    // Получить все митинги
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MeetResponseDto>>> GetMeets()
    {
        try
        {
            var meets = await _context.Meets
                .Include(m => m.Status)
                .Include(m => m.Members) // Загружаем участников
                .Select(m => new MeetResponseDto
                {
                    Uid = m.Uid,
                    Title = m.Title,
                    Date = m.Date,
                    Location = m.Location,
                    Status = new MeetStatusDto
                    {
                        Id = m.Status.Id,
                        Title = m.Status.Title,
                        Description = m.Status.Description
                    },
                    Members = m.Members.Select(member => new MemberResponseDto
                    {
                        Id = member.Id,
                        Name = member.Name,
                        Companion = member.Companion,
                        Contact = member.Contact,
                        Role = member.Role.ToString()
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
                .FirstOrDefaultAsync(m => m.Uid == uid);

            if (meet == null)
                return NotFound();

            return Ok(new MeetResponseDto
            {
                Uid = meet.Uid,
                Title = meet.Title,
                Date = meet.Date,
                Location = meet.Location,
                Status = new MeetStatusDto
                {
                    Id = meet.Status.Id,
                    Title = meet.Status.Title,
                    Description = meet.Status.Description
                },
                Members = meet.Members.Select(member => new MemberResponseDto
                {
                    Id = member.Id,
                    Name = member.Name,
                    Companion = member.Companion,
                    Contact = member.Contact,
                    Role = member.Role.ToString()
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting meet with ID: {uid}");
            return StatusCode(500, "Internal server error");
        }
    }

    // Создать новый митинг
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MeetResponseDto>> CreateMeet([FromBody] MeetCreateDto meetDto)
    {
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
                StatusId = meetDto.StatusId
            };

            _context.Meets.Add(meet);
            await _context.SaveChangesAsync();

            // Добавляем участников, если они есть
            if (meetDto.Members != null && meetDto.Members.Any())
            {
                var members = meetDto.Members.Select(m => new Member
                {
                    Name = m.Name,
                    Companion = m.Companion,
                    Contact = m.Contact,
                    Role = m.Role,
                    MeetGuid = meet.Uid
                }).ToList();

                _context.Member.AddRange(members);
                await _context.SaveChangesAsync();
            }

            // Загружаем созданную встречу с участниками
            var createdMeet = await _context.Meets
                .Include(m => m.Status)
                .Include(m => m.Members)
                .FirstAsync(m => m.Uid == meet.Uid);

            return CreatedAtAction(nameof(GetMeet), 
                new { uid = createdMeet.Uid },
                new MeetResponseDto
                {
                    Uid = createdMeet.Uid,
                    Title = createdMeet.Title,
                    Date = createdMeet.Date,
                    Location = createdMeet.Location,
                    Status = new MeetStatusDto
                    {
                        Id = createdMeet.Status.Id,
                        Title = createdMeet.Status.Title,
                        Description = createdMeet.Status.Description
                    },
                    Members = createdMeet.Members.Select(member => new MemberResponseDto
                    {
                        Id = member.Id,
                        Name = member.Name,
                        Companion = member.Companion,
                        Contact = member.Contact
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