using ease_intro_api.Core.Repository;
using ease_intro_api.Core.Services;
using Microsoft.AspNetCore.Mvc;
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
    private readonly MemberRepository _memberRepository;
    private readonly MemberService _memberService;
    private readonly MeetRepository _meetRepository;

    private readonly string _url;

    public MembersController(
        ApplicationDbContext context,
        ILogger<MembersController> logger,
        MemberRepository memberRepository,
        MemberService memberService,
        MeetRepository meetRepository
        )
    {
        _context = context;
        _logger = logger;
        _memberRepository = memberRepository;
        _memberService = memberService;
        _meetRepository = meetRepository;
        
        _url = "https://your-domain.com/api/members/qrcode/";
    }

    // GET: api/members
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MemberResponseDto>>> GetMembers()
    {
        try
        {
            return Ok(await _memberService.ShowAllMembersAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения участников встречи");
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
            var member = await _memberRepository.GetMemberByIdOrNullAsync(id);
            if (member == null) { return NotFound(); }
            
            return Ok(MemberMapper.MapToDto(member));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Нет участника с укзаным идентификатором.");
            return StatusCode(500, "Internal server error");
        }
    }

    /**
     * <p>Регистрация участника на встречу</p>
     * <p>После прохождения регистрации пользователю в ответ приходит QR для предъвления.</p>
     */
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MemberResponseDto>> CreateMember([FromBody] CreateMemberDto dto)
    {
        try
        {
            var meet = await _meetRepository.GetMeetByUidOrNullAsync(dto.MeetUid);
            if (meet == null) { return BadRequest("Встречи с указаным идентификатором не найдено."); }
            var member = await _memberRepository.CreateMember(dto);
            byte[] pngBytes = ProcessingQr.GenerateQrPng($"{_url}{member.QrCode}");
            
            return File(pngBytes, "image/png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка регистрации на встречу.");
            return StatusCode(500, "Internal server error");
        }
    }
    
    /**
     * Согласно RESTful стандартам, не нужно дополнительное содержимое при успешном обновлении.
     */
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMember(int id, [FromBody] UpdateMemberDto dto)
    {
        try
        {
            var member = await _memberRepository.GetMemberByIdAsync(id);
            await _memberRepository.UpdateMember(dto, member);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обновления данных для участника с указаным идентификатором.");
            return StatusCode(500, "Internal server error");
        }
    }
    
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMember(int id)
    {
        try
        {
            var member = await _memberRepository.GetMemberByIdAsync(id);
            _context.Member.Remove(member);
            await _context.SaveChangesAsync();
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка удаления участника встречи с указаным идентификатором.");
            return StatusCode(500, "Internal server error");
        }
    }
    
    // GET: api/members/qrcode/{qrcode}
    [HttpGet("qrcode/{qrcode}")]
    public async Task<IActionResult> GetMemberByQr(string qrcode)
    {
        try
        {
            var member = await _memberRepository.GetMemberByQrCodeOrNullAsync(qrcode);
            if (member == null) { return NotFound("QR не найден."); }
            
            return Ok(MemberMapper.MapToDto(member));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка в процессе получения участника встречи по указанному идентификатору.");
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
        byte[] pngBytes = ProcessingQr.GenerateQrPng($"{_url}{qrcode}");
        return File(pngBytes, "image/png");
    }
    
    [HttpGet("qrcode/image/{qrcode}/download")]
    public IActionResult DownloadQrImage(string qrcode)
    {
        byte[] pngBytes = ProcessingQr.GenerateQrPng($"{_url}{qrcode}");
        return File(pngBytes, "image/png", "QR код для предъявления.");
    }
}