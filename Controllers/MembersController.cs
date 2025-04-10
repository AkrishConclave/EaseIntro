using ease_intro_api.Core.Services.QR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ease_intro_api.Models;
using ease_intro_api.Data;
using ease_intro_api.DTOs.Member;
using ease_intro_api.Mappers;

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
                .Include(m => m.Meet)
                .ThenInclude(m => m!.Owner)
                .Include(m => m.Meet)
                .ThenInclude(m => m!.Status)
                .AsNoTracking()
                .ToListAsync();

            return members.Select(MemberMapper.MapToDto).ToList();
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
                .ThenInclude(meet => meet!.Status)
                .Include(m => m.Meet)
                .ThenInclude(meet => meet!.Owner)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (member == null) { return NotFound(); }
            
            return Ok(MemberMapper.MapToDto(member));
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
                MeetGuid = dto.MeetUid,
                Role = Member.MemberRole.Guest,
                QrCode = ProcessingQr.GenerateQr(dto.MeetUid)
            };

            _context.Member.Add(member);
            await _context.SaveChangesAsync();
            
            member = await _context.Member
                .Include(m => m.Meet)
                .ThenInclude(meet => meet!.Status)
                .Include(m => m.Meet)
                .ThenInclude(meet => meet!.Owner)
                .FirstOrDefaultAsync(m => m.Id == member.Id);

            return CreatedAtAction(nameof(GetMember), new { id = member!.Id }, MemberMapper.MapToDto(member));
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
            if (member == null) { return NotFound(); }

            if (!string.IsNullOrEmpty(dto.Name)) { member.Name = dto.Name; }
            if (!string.IsNullOrEmpty(dto.Companion)) { member.Companion = dto.Companion; }
            if (!string.IsNullOrEmpty(dto.Contact)) { member.Contact = dto.Contact; }
            if (dto.Role.HasValue)
            {
                if (Enum.IsDefined(typeof(Member.MemberRole), dto.Role.Value))
                {
                    member.Role = (Member.MemberRole)dto.Role.Value;
                }
                else { return BadRequest("Недопустимое значение роли."); }
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
            if (member == null) { return NotFound(); }

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
    
    // GET: api/members/qrcode/{qrcode}
    [HttpGet("qrcode/{qrcode}")]
    public async Task<IActionResult> GetMemberByQr(string qrcode)
    {
        try
        {
            var member = await _context.Member
                .Include(m => m.Meet)
                .Include(m => m.Meet!.Status)
                .Include(m => m.Meet!.Owner)
                .FirstOrDefaultAsync(m => m.QrCode == qrcode);

            if (member == null) { return NotFound("Member not found"); }
            
            return Ok(MemberMapper.MapToDto(member));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting member by QR code: {qrcode}");
            return StatusCode(500, "Internal server error");
        }
    }
    
    /**
     * <p>Метод вернет по ссылке http://localhost:5215/api/members/qrcode/image/qrcode
     * изображение `qr` кода который нужно вернуть пользователю при регистрации на событие.</p>
     * <p>При первом запросе с qr кода, необходимо будет считать его недействительным.</p>
     * <p>todo QR коды должны быть одноразовыми, затем его статус становиться `false`</p>
     * <p>todo Решить как работать с url, что бы было разделение dev, и prod</p>
     * <param name="qrcode">QR код который привязан к участнику, получаемый из `url`</param>
     * <returns>Изображение QR кода</returns>
     */
    [HttpGet("qrcode/image/{qrcode}")]
    public IActionResult GetQrImage(string qrcode)
    {
        var url = $"https://your-domain.com/api/members/qrcode/{qrcode}";
        byte[] pngBytes = ProcessingQr.GenerateQrPng(url);
        return File(pngBytes, "image/png");
    }
    
    [HttpGet("qrcode/image/{qrcode}/download")]
    public IActionResult DownloadQrImage(string qrcode)
    {
        var url = $"https://your-domain.com/api/members/qrcode/{qrcode}";
        byte[] pngBytes = ProcessingQr.GenerateQrPng(url);
        return File(pngBytes, "image/png", "QR код для показа");
    }
}