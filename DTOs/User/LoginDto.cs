using System.ComponentModel.DataAnnotations;

namespace ease_intro_api.DTOs.User;

public class LoginDto
{
    [Required]
    [EmailAddress]
    [StringLength(90)]
    public string UserEmail { get; set; } = null!;
    
    [Required]
    [StringLength(90)]
    public string Password { get; set; } = null!;
}