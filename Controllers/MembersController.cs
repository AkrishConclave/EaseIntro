using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ease_intro_api.Models;
using ease_intro_api.Data;
using ease_intro_api.DTOs;
using ease_intro_api.DTOs.Meet;
using ease_intro_api.DTOs.Member;
using ease_intro_api.DTOs.User;
using QRCoder;

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
                    Name = m.Name,
                    Companion = m.Companion,
                    Contact = m.Contact,
                    Role = m.Role.ToString(),
                    QrCode = m.QrCode,
                    Meet = new MeetResponseDto
                    {
                        Uid = m.Meet!.Uid,
                        Title = m.Meet.Title,
                        Date = m.Meet.Date,
                        Location = m.Meet.Location,
                        LimitMembers = m.Meet.LimitMembers,
                        AllowedPlusOne = m.Meet.AllowedPlusOne,
                        Owner = new UserResponseDto
                        {
                            PublicName = m.Meet.Owner.PublicName,
                            PublicContact = m.Meet.Owner.PublicContact,
                        },
                        Status = new MeetStatusDto
                        {
                            Title = m.Meet.Status!.Title,
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
                .ThenInclude(meet => meet!.Status)
                .Include(m => m.Meet)
                .ThenInclude(meet => meet!.Owner)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (member == null)
            {
                return NotFound();
            }

            return Ok(new MemberResponseDto
            {
                Name = member.Name,
                Companion = member.Companion,
                Contact = member.Contact,
                Role = member.Role.ToString(),
                QrCode = member.QrCode,
                Meet = new MeetResponseDto
                {
                    Uid = member.Meet!.Uid,
                    Title = member.Meet.Title,
                    Date = member.Meet.Date,
                    Location = member.Meet.Location,
                    LimitMembers = member.Meet.LimitMembers,
                    AllowedPlusOne = member.Meet.AllowedPlusOne,
                    Owner = new UserResponseDto
                    {
                        PublicName = member.Meet.Owner.PublicName,
                        PublicContact = member.Meet.Owner.PublicContact,
                    },
                    Status = new MeetStatusDto
                    {
                        Title = member.Meet.Status!.Title,
                        Description = member.Meet.Status.Description
                    }
                }
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

            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            
            var member = new Member
            {
                Name = dto.Name,
                Companion = dto.Companion,
                Contact = dto.Contact,
                MeetGuid = dto.MeetUid,
                QrCode = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{timestamp}-{dto.MeetUid}"))
            };

            _context.Member.Add(member);
            await _context.SaveChangesAsync();

            var responseDto = new MemberResponseDto
            {
                Name = member.Name,
                Companion = member.Companion,
                Contact = member.Contact,
                Role = member.Role.ToString(),
                QrCode = member.QrCode,
                Meet = member.Meet != null
                    ? new MeetResponseDto
                    {
                        Uid = member.Meet.Uid,
                        Title = member.Meet.Title,
                        Date = member.Meet.Date,
                        Location = member.Meet.Location,
                        LimitMembers = member.Meet.LimitMembers,
                        AllowedPlusOne = member.Meet.AllowedPlusOne,
                        Owner = new UserResponseDto
                        {
                            PublicName = member.Meet.Owner.PublicName,
                            PublicContact = member.Meet.Owner.PublicContact,
                        },
                        Status = member.Meet.Status != null
                            ? new MeetStatusDto
                            {
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

            if (member == null)
                return NotFound("Member not found");

            var dto = new MemberResponseDto
            {
                Name = member.Name,
                Companion = member.Companion,
                Contact = member.Contact,
                Role = member.Role.ToString(),
                QrCode = member.QrCode,
                Meet = new MeetResponseDto
                {
                    Uid = member.Meet!.Uid,
                    Title = member.Meet.Title,
                    Date = member.Meet.Date,
                    Location = member.Meet.Location,
                    LimitMembers = member.Meet.LimitMembers,
                    AllowedPlusOne = member.Meet.AllowedPlusOne,
                    Owner = new UserResponseDto
                    {
                        PublicName = member.Meet.Owner.PublicName,
                        PublicContact = member.Meet.Owner.PublicContact,
                    },
                    Status = new MeetStatusDto
                    {
                        Title = member.Meet.Status!.Title,
                        Description = member.Meet.Status.Description
                    }
                }
            };

            return Ok(dto);
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

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);

        var pngQrCode = new PngByteQRCode(qrCodeData);
        byte[] pngBytes = pngQrCode.GetGraphic(42);
        return File(pngBytes, "image/png");
    }
}