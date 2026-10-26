using ease_intro_api.Core.Services.Interfaces;
using ease_intro_api.Data;
using ease_intro_api.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ease_intro_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "User")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UsersController> _logger;
    private readonly IUserContextService _userContextService;

    public UsersController(
        ApplicationDbContext context,
        ILogger<UsersController> logger,
        IUserContextService userContextService)
    {
        _context = context;
        _logger = logger;
        _userContextService = userContextService;
    }

    /// <summary>
    /// Получить информацию о текущем пользователе.
    /// </summary>
    /// <returns>Информация о текущем пользователе.</returns>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResponseUserDto>> GetMe()
    {
        var userId = _userContextService.UserId;

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound("Пользователь не найден.");

        return Ok(new ResponseUserDto
        {
            PublicName = user.PublicName,
            PublicContact = user.PublicContact
        });
    }

    /// <summary>
    /// Обновить информацию о текущем пользователе.
    /// </summary>
    /// <param name="dto">Данные для обновления профиля.</param>
    /// <returns>Результат обновления.</returns>
    [HttpPut("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateUserDto dto)
    {
        var userId = _userContextService.UserId;

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound("Пользователь не найден.");

        user.PublicName = dto.PublicName ?? user.PublicName;
        user.PublicContact = dto.PublicContact ?? user.PublicContact;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Изменить пароль текущего пользователя.
    /// </summary>
    /// <param name="dto">Данные для смены пароля (старый и новый пароль).</param>
    /// <returns>Результат смены пароля.</returns>
    [HttpPut("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = _userContextService.UserId;

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound("Пользователь не найден.");

        // Проверка старого пароля
        if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
            return BadRequest("Неверный текущий пароль.");

        // Валидация нового пароля
        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
            return BadRequest("Новый пароль должен содержать минимум 6 символов.");

        // Хеширование нового пароля
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
