using System.ComponentModel.DataAnnotations;

namespace ease_intro_api.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserName { get; set; }

    [Required]
    public string PasswordHash { get; set; }

    public string Role { get; set; } = "User";
}