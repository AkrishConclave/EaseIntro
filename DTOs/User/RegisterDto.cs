using System.ComponentModel.DataAnnotations;

namespace ease_intro_api.DTOs.User;

public class RegisterDto
{
    [Required]
    [StringLength(90)]
    public string UserEmail { get; set; } = null!;

    [Required]
    [StringLength(90)]
    public string Password { get; set; } = null!;
    
    [StringLength(200)]
    public string PublicName { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string PublicContact { get; set; } = string.Empty;
    
}