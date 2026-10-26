using System.Security.Claims;
using ease_intro_api.Data.Repository;
using ease_intro_api.Core.Services;
using Microsoft.AspNetCore.Mvc;
using ease_intro_api.Data;
using ease_intro_api.DTOs.Member;
using Microsoft.AspNetCore.Authorization;
using ease_intro_api.Mappers;
using Microsoft.EntityFrameworkCore;

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

    private readonly string _baseUrl;

    public MembersController
    (
        ApplicationDbContext context,
        ILogger<MembersController> logger,
        MemberRepository memberRepository,
        MemberService memberService,
        MeetRepository meetRepository,
        IConfiguration configuration
    )
    {
        _context = context;
        _logger = logger;
        _memberRepository = memberRepository;
        _memberService = memberService;
        _meetRepository = meetRepository;
        
        _baseUrl = configuration["BaseUrl"]?.TrimEnd('/') ?? "https://your-domain.com";
    }

    /// <summary>
    /// Маскирует email участника для безопасного отображения.
    /// </summary>
    private static string MaskEmail(string contact)
    {
        if (string.IsNullOrWhiteSpace(contact))
            return string.Empty;

        var parts = contact.Split('@');
        if (parts.Length == 2)
        {
            var local = parts[0];
            var domain = parts[1];
            if (local.Length > 0)
            {
                return $"{local[0]}***@{domain}";
            }
        }
        return "***";
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
            if (await _memberRepository.CheckExistsContactAsync(dto.Contact, dto.MeetUid)) { return NotFound("Контакт с такой почтой уже зарегистрирован."); }
            var meet = await _meetRepository.PublicGetMeetByUidOrNullAsync(dto.MeetUid);
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
            
            // Дополнительная проверка владельца (ShowMemberByIdOrNullAsync уже проверяет, но для безопасности оставляем)
            var meet = await _meetRepository.PublicGetMeetByUidOrNullAsync(member.MeetGuid);
            if (meet == null)
                return NotFound("Встреча не найдена.");
            
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Forbid();

            if (!int.TryParse(userIdClaim, out var userId))
                return StatusCode(500, "Некорректный токен (userId не int).");

            if (meet.OwnerId != userId)
                return Forbid();
            
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
    [Authorize(Roles = "User")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteMember(int id)
    {
        try
        {
            var member = await _memberRepository.GetMemberByIdOrNullAsync(id);
            if (member == null)
                return NotFound("Участник с указанным идентификатором не найден.");
            
            // Проверка владельца встречи
            var meet = await _meetRepository.PublicGetMeetByUidOrNullAsync(member.MeetGuid);
            if (meet == null)
                return NotFound("Встреча не найдена.");
            
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Forbid();

            if (!int.TryParse(userIdClaim, out var userId))
                return StatusCode(500, "Некорректный токен (userId не int).");

            if (meet.OwnerId != userId)
                return Forbid();
            
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
    /// Получить информацию об участнике по его контакту.
    /// </summary>
    /// <remarks>
    /// Этот метод позволяет получить данные о конкретном участнике.
    /// Если участник не найден, будет возвращен соответствующий HTTP-ответ.
    /// Доступно авторизованным пользователям с ролью "User".
    /// Возвращает билеты пользователя (если contact совпадает с email авторизованного пользователя),
    /// либо участников встреч, созданных текущим пользователем.
    /// </remarks>
    /// <param name="contact">Контакт участника, для которого требуется получить информацию.</param>
    /// <returns>
    /// Возвращает информацию об участнике в случае успешного выполнения (200 OK).
    /// Возвращает 401 Unauthorized, если пользователь не авторизован.
    /// Возвращает 500 Internal Server Error, если произошла ошибка на сервере.
    /// </returns>
    [HttpGet("participant/{contact}")]
    [Authorize(Roles = "User")]
    [ProducesResponseType(typeof(IEnumerable<ResponseMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMemberMeets(string contact)
    {
        try
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.Name)?.Value;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var members = await _memberRepository.GetMemberByContactAsync(contact);

            if (!string.Equals(contact, currentUserEmail, StringComparison.OrdinalIgnoreCase))
            {
                members = members.Where(m => m.Meet != null && m.Meet.OwnerId == userId).ToList();
            }

            var membersDto = members.Select(MemberMapper.MapToDto).ToList();
        
            return Ok(membersDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка в процессе получения участников встречи по указанному контакту.");
            return StatusCode(500, "Internal server error");
        }
    }
    
    /// <summary>
    /// Безопасный (идемпотентный) предпросмотр статуса билета по QR-коду.
    /// </summary>
    /// <remarks>
    /// Публичный endpoint для получения информации о билете без его гашения (IsCheckedIn не меняется).
    /// Маскирует полный контакт участника.
    /// </remarks>
    /// <param name="qrcode">QR-код участника встречи.</param>
    /// <returns>
    /// Информация о билете и статус готовности к отметке.
    /// </returns>
    [HttpGet("qrcode/{qrcode}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ScanMemberQr(string qrcode)
    {
        try
        {
            var member = await _memberRepository.GetMemberByQrCodeOrNullAsync(qrcode);
            if (member == null)
                return NotFound("QR-код не найден.");
            
            var meet = await _meetRepository.PublicGetMeetByUidOrNullAsync(member.MeetGuid);
            if (meet == null)
                return NotFound("Встреча не найдена.");
            
            var now = DateTime.UtcNow;
            var meetDate = meet.Date.ToUniversalTime();
            
            if (member.IsCheckedIn)
            {
                return Ok(new
                {
                    member = new
                    {
                        name = member.Name,
                        companion = member.Companion,
                        contact = MaskEmail(member.Contact),
                        role = member.Role.ToString()
                    },
                    meetInfo = new
                    {
                        title = meet.Title,
                        date = meet.Date,
                        location = meet.Location
                    },
                    qrValid = false,
                    message = "QR-код уже использован. Участник был отмечен ранее.",
                    checkedInAt = member.CheckedInAt,
                    isCheckedIn = true
                });
            }

            bool canCheckIn = false;
            string message;

            bool hasTime = meetDate.TimeOfDay != TimeSpan.Zero;

            if (hasTime)
            {
                var threeHoursBefore = meetDate.AddHours(-3);
                var meetDayEnd = new DateTime(meetDate.Year, meetDate.Month, meetDate.Day, 23, 59, 59, DateTimeKind.Utc);

                canCheckIn = now >= threeHoursBefore && now <= meetDayEnd;

                if (now < threeHoursBefore)
                {
                    message = $"Сканирование возможно не ранее {threeHoursBefore:dd.MM.yyyy HH:mm} (за 3 часа до начала мероприятия)";
                }
                else if (now > meetDayEnd)
                {
                    message = "Время сканирования истекло";
                }
                else
                {
                    message = "QR-код действителен";
                }
            }
            else
            {
                var meetDayStart = new DateTime(meetDate.Year, meetDate.Month, meetDate.Day, 0, 0, 0, DateTimeKind.Utc);
                var meetDayEnd = meetDayStart.AddDays(1).AddSeconds(-1);

                canCheckIn = now >= meetDayStart && now <= meetDayEnd;

                if (now < meetDayStart)
                {
                    message = $"Сканирование возможно только {meetDayStart:dd.MM.yyyy} (в день мероприятия)";
                }
                else if (now > meetDayEnd)
                {
                    message = "Время сканирования истекло";
                }
                else
                {
                    message = "QR-код действителен";
                }
            }

            return Ok(new
            {
                member = new
                {
                    name = member.Name,
                    companion = member.Companion,
                    contact = MaskEmail(member.Contact),
                    role = member.Role.ToString()
                },
                meetInfo = new
                {
                    title = meet.Title,
                    date = meet.Date,
                    location = meet.Location
                },
                qrValid = canCheckIn,
                message = message,
                checkedInAt = (DateTime?)null,
                isCheckedIn = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при предпросмотре QR-кода участника.");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Фиксация прохода участника по QR-коду (Check-In).
    /// </summary>
    /// <remarks>
    /// Фиксирует проход участника (IsCheckedIn = true). Доступно владельцу встречи.
    /// </remarks>
    /// <param name="qrcode">QR-код участника встречи для отметки прихода.</param>
    /// <returns>
    /// Результат фиксации прохода.
    /// </returns>
    [HttpPost("qrcode/{qrcode}/check-in")]
    [Authorize(Roles = "User")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckInMemberQr(string qrcode)
    {
        try
        {
            var member = await _memberRepository.GetMemberByQrCodeOrNullAsync(qrcode);
            if (member == null)
                return NotFound("QR-код не найден.");

            var meet = await _meetRepository.PublicGetMeetByUidOrNullAsync(member.MeetGuid);
            if (meet == null)
                return NotFound("Встреча не найдена.");

            // Проверка прав (владелец встречи)
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId) || meet.OwnerId != userId)
            {
                return Forbid();
            }

            if (member.IsCheckedIn)
            {
                return Ok(new
                {
                    member = new
                    {
                        name = member.Name,
                        companion = member.Companion,
                        contact = MaskEmail(member.Contact),
                        role = member.Role.ToString()
                    },
                    meetInfo = new
                    {
                        title = meet.Title,
                        date = meet.Date,
                        location = meet.Location
                    },
                    qrValid = false,
                    message = "QR-код уже использован. Участник был отмечен ранее.",
                    checkedInAt = member.CheckedInAt,
                    isCheckedIn = true
                });
            }
            
            var now = DateTime.UtcNow;
            var meetDate = meet.Date.ToUniversalTime();

            bool canCheckIn = false;
            string message;
            
            bool hasTime = meetDate.TimeOfDay != TimeSpan.Zero;
            
            if (hasTime)
            {
                var threeHoursBefore = meetDate.AddHours(-3);
                var meetDayEnd = new DateTime(meetDate.Year, meetDate.Month, meetDate.Day, 23, 59, 59, DateTimeKind.Utc);
                
                canCheckIn = now >= threeHoursBefore && now <= meetDayEnd;
                
                if (now < threeHoursBefore)
                {
                    message = $"Сканирование возможно не ранее {threeHoursBefore:dd.MM.yyyy HH:mm} (за 3 часа до начала мероприятия)";
                }
                else if (now > meetDayEnd)
                {
                    message = "Время сканирования истекло";
                }
                else
                {
                    message = "Участник успешно отмечен";
                }
            }
            else
            {
                var meetDayStart = new DateTime(meetDate.Year, meetDate.Month, meetDate.Day, 0, 0, 0, DateTimeKind.Utc);
                var meetDayEnd = meetDayStart.AddDays(1).AddSeconds(-1);
                
                canCheckIn = now >= meetDayStart && now <= meetDayEnd;
                
                if (now < meetDayStart)
                {
                    message = $"Сканирование возможно только {meetDayStart:dd.MM.yyyy} (в день мероприятия)";
                }
                else if (now > meetDayEnd)
                {
                    message = "Время сканирования истекло";
                }
                else
                {
                    message = "Участник успешно отмечен";
                }
            }
            
            if (canCheckIn)
            {
                await _memberRepository.CheckInMemberAsync(member);
                member = await _memberRepository.GetMemberByQrCodeOrNullAsync(qrcode);
            }
            
            return Ok(new
            {
                member = new
                {
                    name = member!.Name,
                    companion = member.Companion,
                    contact = MaskEmail(member.Contact),
                    role = member.Role.ToString()
                },
                meetInfo = new
                {
                    title = meet.Title,
                    date = meet.Date,
                    location = meet.Location
                },
                qrValid = !canCheckIn && !member.IsCheckedIn,
                message = message,
                checkedInAt = member.IsCheckedIn ? member.CheckedInAt : null,
                isCheckedIn = member.IsCheckedIn
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отметке прихода участника по QR-коду.");
            return StatusCode(500, "Internal server error");
        }
    }
    
    /// <summary>
    /// Получить изображение QR-кода для регистрации на событие.
    /// </summary>
    /// <remarks>
    /// Этот метод возвращает изображение QR-кода, связанное с участником, по предоставленному QR-коду.
    /// Возвращает 404 Not Found, если QR-код не найден в базе данных.
    /// </remarks>
    /// <param name="qrcode">QR-код, привязанный к участнику, получаемый из URL.</param>
    /// <returns>
    /// Изображение QR-кода для предъявления пользователю в формате PNG.
    /// </returns>
    [HttpGet("qrcode/image/{qrcode}")]
    [Produces("image/png")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQrImage(string qrcode)
    {
        try
        {
            var member = await _memberRepository.GetMemberByQrCodeOrNullAsync(qrcode);
            if (member == null)
            {
                return NotFound("QR-код не найден.");
            }

            byte[] pngBytes = ProcessingQrService.GenerateQrPng($"{_baseUrl}/api/members/qrcode/{qrcode}");
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
    /// Возвращает 404 Not Found, если QR-код не найден в базе данных.
    /// </remarks>
    /// <param name="qrcode">QR-код, привязанный к участнику, получаемый из URL.</param>
    /// <returns>
    /// Файл с изображением QR-кода в формате PNG для скачивания.
    /// </returns>
    [HttpGet("qrcode/image/{qrcode}/download")]
    [Produces("image/png")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadQrImage(string qrcode)
    {
        try
        {
            var member = await _memberRepository.GetMemberByQrCodeOrNullAsync(qrcode);
            if (member == null)
            {
                return NotFound("QR-код не найден.");
            }

            byte[] pngBytes = ProcessingQrService.GenerateQrPng($"{_baseUrl}/api/members/qrcode/{qrcode}");
            return File(pngBytes, "image/png", $"qr_{qrcode}.png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при скачивании QR-кода.");
            return StatusCode(500, "Internal server error");
        }
    }
    
}