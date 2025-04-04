using ease_intro_api.Data.Repository;
using ease_intro_api.Core.Services;
using Microsoft.AspNetCore.Mvc;
using ease_intro_api.Data;
using ease_intro_api.DTOs.Member;
using Microsoft.AspNetCore.Authorization;
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

    public MembersController
    (
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
    
    /// <summary>
    /// Регистрирует участника на встречу.
    /// </summary>
    /// <param name="dto">Объект с данными участника для регистрации.</param>
    /// <returns>Уникальный идентификатор учатника.</returns>
    /// <remarks>Если встреча не найдена — возвращается BadRequest.</remarks>
    /// <example>
    /// Пример запроса:<br/>
    /// POST /api/members<br/>
    /// {"name": "Иванов Иван", "companion": "С супругой", "contact": "email@email.ru", "meetUid": "3fa85f64-5717-4562-b3fc-2c963f66afa6"}
    /// </example>
    /// <exception cref="ArgumentNullException">Если DTO равен null.</exception>
    /// <seealso cref="GetQrImage(string)"/>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResponseMemberDto>> CreateMember([FromBody] CreateMemberDto dto)
    {
        try
        {
            var meet = await _meetRepository.GetMeetByUidOrNullAsync(dto.MeetUid);
            if (meet == null) { return NotFound("Встречи с указаным идентификатором не найдено."); }
            var member = await _memberRepository.CreateMember(dto);
            
            return Ok($"Это ваш уникальный идентификатор: {member.QrCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка регистрации на встречу.");
            return StatusCode(500, "Internal server error");
        }
    }
    
    /// <summary>
    /// Получить информацию об участнике по его идентификатору.
    /// </summary>
    /// <remarks>
    /// Этот метод позволяет получить данные о конкретном участнике встречи, если он существует и принадлежит текущему пользователю.
    /// Если участник не найден или пользователь не авторизован, будет возвращен соответствующий HTTP-ответ.
    /// </remarks>
    /// <param name="id">Идентификатор участника, для которого требуется получить информацию.</param>
    /// <returns>
    /// Возвращает информацию об участнике в случае успешного выполнения (200 OK).
    /// Возвращает 401 Unauthorized, если пользователь не авторизован.
    /// Возвращает 404 Not Found, если участник не найден.
    /// Возвращает 500 Internal Server Error, если произошла ошибка на сервере.
    /// </returns>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "User")]
    [ProducesResponseType(typeof(ResponseMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResponseMemberDto>> GetMember(int id)
    {
        try
        {
            var member = await _memberService.ShowMemberByIdOrNullAsync(id);
            if (member == null) { return NotFound("Не найден участник с данным идентификатором в ваших встречах."); }
            
            return Ok(MemberMapper.MapToDto(member));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Нет участника с укзаным идентификатором.");
            return StatusCode(500, "Internal server error");
        }
    }
    
    /// <summary>
    /// Редактирование участника встречи.
    /// </summary>
    /// <remarks>
    /// Этот метод позволяет обновить информацию о участнике встречи, например, имя, контактные данные и роль.
    /// При успешном обновлении возвращается код 204 No Content, что означает успешное выполнение без дополнительного содержимого.
    /// Изменять данные участников можно только на те встречи, которые создал авторизированный пользователь.
    /// </remarks>
    /// <param name="id">Идентификатор участника встречи.</param>
    /// <param name="dto">Объект с данными для обновления участника.</param>
    /// <returns>
    /// Возвращает:
    /// - 204 No Content, если обновление прошло успешно.
    /// - 400 Bad Request, если данные запроса неверны.
    /// - 401 Unauthorized, если пользователь не авторизован.
    /// - 403 Forbidden, если у пользователя нет прав на изменение участника.
    /// - 404 Not Found, если участник с данным идентификатором не найден в рамках встречи.
    /// </returns>
    [HttpPut("{id}")]
    [Authorize(Roles = "User")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMember(int id, [FromBody] UpdateMemberDto dto)
    {
        try
        {
            var member = await _memberService.ShowMemberByIdOrNullAsync(id);
            if (member == null) { return NotFound("Не найден участник с данным идентификатором в ваших встречах."); }
            await _memberRepository.UpdateMemberAsync(dto, member);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обновления данных для участника с указаным идентификатором.");
            return StatusCode(500, "Internal server error");
        }
    }
    
    /// <summary>
    /// Удаление участника по идентификатору.
    /// </summary>
    /// <remarks>
    /// Этот метод удаляет участника по его идентификатору (ID). 
    /// Если участника с указанным идентификатором не найдено, возвращается ошибка.
    /// </remarks>
    /// <param name="id">Уникальный идентификатор участника.</param>
    /// <returns>Результат удаления. При успешном удалении возвращается код 204 No Content.</returns>
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
    
    /// <summary>
    /// Получить информацию об участнике по QR-коду.
    /// </summary>
    /// <remarks>
    /// Этот метод позволяет получить информацию о конкретном участнике встречи, используя его QR-код.
    /// Если участник не найден по предоставленному QR-коду, будет возвращен ответ с кодом 404.
    /// </remarks>
    /// <param name="qrcode">QR-код участника встречи, по которому необходимо получить информацию.</param>
    /// <returns>
    /// Возвращает информацию об участнике в случае успешного выполнения (200 OK).
    /// Возвращает 404 Not Found, если участник не найден по QR-коду.
    /// Возвращает 500 Internal Server Error, если произошла ошибка на сервере.
    /// </returns>
    [HttpGet("qrcode/{qrcode}")]
    [ProducesResponseType(typeof(ResponseMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)] 
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
    
    /// <summary>
    /// Получить изображение QR-кода для регистрации на событие.
    /// </summary>
    /// <remarks>
    /// Этот метод возвращает изображение QR-кода, связанное с участником, по предоставленному QR-коду.
    /// При первом запросе QR-код должен быть действительным. В дальнейшем его статус становится "недействительным".
    /// Также, необходимо продумать разделение URL для разных сред (разработка, продакшн).
    /// </remarks>
    /// <param name="qrcode">QR-код, привязанный к участнику, получаемый из URL.</param>
    /// <returns>
    /// Изображение QR-кода для предъявления пользователю в формате PNG.
    /// Возвращается с кодом 200 OK при успешном запросе.
    /// </returns>
    [HttpGet("qrcode/image/{qrcode}")]
    [Produces("image/png")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FileContentResult),StatusCodes.Status404NotFound)]
    public IActionResult GetQrImage(string qrcode)
    {
        try
        {
            byte[] pngBytes = ProcessingQrService.GenerateQrPng($"{_url}{qrcode}");
            return File(pngBytes, "image/png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при генерации QR-кода.");
            return StatusCode(500, "Internal server error");
        }
    }
    
    /// <summary>
    /// Скачать изображение QR-кода для регистрации на событие.
    /// </summary>
    /// <remarks>
    /// Этот метод позволяет пользователю скачать изображение QR-кода, связанное с участником, по предоставленному QR-коду.
    /// Пользователь может сохранить этот QR-код для предъявления на событии.
    /// </remarks>
    /// <param name="qrcode">QR-код, привязанный к участнику, получаемый из URL.</param>
    /// <returns>
    /// Файл с изображением QR-кода в формате PNG для скачивания.
    /// Возвращается с кодом 200 OK при успешном запросе.
    /// </returns>
    [HttpGet("qrcode/image/{qrcode}/download")]
    [Produces("image/png")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FileContentResult),StatusCodes.Status404NotFound)]
    public IActionResult DownloadQrImage(string qrcode)
    {
        try
        {
            byte[] pngBytes = ProcessingQrService.GenerateQrPng($"{_url}{qrcode}");
            return File(pngBytes, "image/png", "QR код для предъявления.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при скачивании QR-кода.");
            return StatusCode(500, "Internal server error");
        }
    }
}