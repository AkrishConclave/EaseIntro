using System.ComponentModel.DataAnnotations;

namespace ease_intro_api.DTOs;

public class LoginDto
{
    [Required]
    [StringLength(90)]
    public string Username { get; set; } = null!;
    
    [Required]
    [StringLength(90)]
    public string Password { get; set; } = null!;
}