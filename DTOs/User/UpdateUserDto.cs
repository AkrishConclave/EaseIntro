using System.ComponentModel.DataAnnotations;

namespace ease_intro_api.DTOs.User;

public class UpdateUserDto
{
    [StringLength(200)]
    public string? PublicName { get; set; }
    
    [StringLength(200)]
    public string? PublicContact { get; set; }
}


