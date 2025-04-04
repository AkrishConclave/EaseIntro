using System.ComponentModel.DataAnnotations;

namespace ease_intro_api.DTOs.User;

public class LoginUserDto
{
    [Required]
    [EmailAddress]
    [StringLength(90)]
    public string UserEmail { get; set; } = null!;
    
    [Required]
    [StringLength(90)]
    public string Password { get; set; } = null!;
}