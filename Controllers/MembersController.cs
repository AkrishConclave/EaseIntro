using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ease_intro_api.Models;
using ease_intro_api.Data;
using ease_intro_api.DTOs;
using ease_intro_api.DTOs.Meet;
using ease_intro_api.DTOs.Member;

namespace ease_intro_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MembersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MembersController> _logger;

    public MembersController(
        ApplicationDbContext context,
        ILogger<MembersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/members
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MemberResponseDto>>> GetMembers()
    {
        try
        {
            var members = await _context.Member
                .Select(m => new MemberResponseDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Companion = m.Companion,
                    Contact = m.Contact,
                    Meet = new MeetResponseDto
                    {
                        Uid = m.Meet.Uid,
                        Title = m.Meet.Title,
                        Date = m.Meet.Date,
                        Location = m.Meet.Location,
                        Status = new MeetStatusDto
                        {
                            Id = m.Meet.Status.Id,
                            Title = m.Meet.Status.Title,
                            Description = m.Meet.Status.Description
                        }
                    }
                })
                .ToListAsync();

            return Ok(members);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting members");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberResponseDto>> GetMember(int id)
    {
        try
        {
            var member = await _context.Member
                .Include(m => m.Meet)
                .ThenInclude(meet => meet.Status) // Загружаем MeetStatus
                .FirstOrDefaultAsync(m => m.Id == id);

            if (member == null)
            {
                return NotFound();
            }

            return Ok(new MemberResponseDto
            {
                Id = member.Id,
                Name = member.Name,
                Companion = member.Companion,
                Contact = member.Contact,
                Meet = member.Meet != null
                    ? new MeetResponseDto
                    {
                        Uid = member.Meet.Uid,
                        Title = member.Meet.Title,
                        Date = member.Meet.Date,
                        Location = member.Meet.Location,
                        Status = member.Meet.Status != null
                            ? new MeetStatusDto
                            {
                                Id = member.Meet.Status.Id,
                                Title = member.Meet.Status.Title,
                                Description = member.Meet.Status.Description
                            }
                            : null
                    }
                    : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting member with ID: {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    // POST: api/members
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MemberResponseDto>> CreateMember([FromBody] CreateMemberDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Проверяем существование Meet
            if (!await _context.Meets.AnyAsync(m => m.Uid == dto.MeetUid))
            {
                return BadRequest("Meet not found");
            }

            var member = new Member
            {
                Name = dto.Name,
                Companion = dto.Companion,
                Contact = dto.Contact,
                MeetGuid = dto.MeetUid
            };

            _context.Member.Add(member);
            await _context.SaveChangesAsync();

            var responseDto = new MemberResponseDto
            {
                Id = member.Id,
                Name = member.Name,
                Companion = member.Companion,
                Contact = member.Contact,
                Meet = member.Meet != null
                    ? new MeetResponseDto
                    {
                        Uid = member.Meet.Uid,
                        Title = member.Meet.Title,
                        Date = member.Meet.Date,
                        Location = member.Meet.Location,
                        Status = member.Meet.Status != null
                            ? new MeetStatusDto
                            {
                                Id = member.Meet.Status.Id,
                                Title = member.Meet.Status.Title,
                                Description = member.Meet.Status.Description
                            }
                            : null
                    }
                    : null
            };

            return CreatedAtAction(nameof(GetMember), new { id = member.Id }, responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating member");
            return StatusCode(500, "Internal server error");
        }
    }

    // PUT: api/members/5
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMember(int id, [FromBody] UpdateMemberDto dto)
    {
        try
        {
            var member = await _context.Member.FindAsync(id);
            if (member == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(dto.Name))
            {
                member.Name = dto.Name;
            }

            if (!string.IsNullOrEmpty(dto.Companion))
            {
                member.Companion = dto.Companion;
            }

            if (!string.IsNullOrEmpty(dto.Contact))
            {
                member.Contact = dto.Contact;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating member with ID: {id}");
            return StatusCode(500, "Internal server error");
        }
    }

    // DELETE: api/members/5
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMember(int id)
    {
        try
        {
            var member = await _context.Member.FindAsync(id);
            if (member == null)
            {
                return NotFound();
            }

            _context.Member.Remove(member);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting member with ID: {id}");
            return StatusCode(500, "Internal server error");
        }
    }
}