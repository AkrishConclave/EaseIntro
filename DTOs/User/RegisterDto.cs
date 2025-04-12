using System.ComponentModel.DataAnnotations;

namespace ease_intro_api.DTOs.User;

public class RegisterDto
{
    [Required]
    [EmailAddress]
    [StringLength(160)]
    public string UserEmail { get; set; } = null!;

    [Required]
    [StringLength(512)]
    public string Password { get; set; } = null!;
    
    [StringLength(200)]
    public string PublicName { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string PublicContact { get; set; } = string.Empty;
    
}