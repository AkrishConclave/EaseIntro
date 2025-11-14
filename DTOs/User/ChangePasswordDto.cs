using System.ComponentModel.DataAnnotations;

namespace ease_intro_api.DTOs.User;

public class ChangePasswordDto
{
    [Required]
    [StringLength(512)]
    public string OldPassword { get; set; } = null!;
    
    [Required]
    [StringLength(512)]
    [MinLength(6, ErrorMessage = "Пароль должен содержать минимум 6 символов.")]
    public string NewPassword { get; set; } = null!;
}


