using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ease_intro_api.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(160)]
    public string UserEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(512)]
    public string PasswordHash { get; set; } = string.Empty;
    
    [DefaultValue("Пользователь не указал публичное имя")]
    [StringLength(200)]
    public string PublicName { get; set; } = string.Empty;
    
    [DefaultValue("Пользователь не указал данные для связи")]
    [StringLength(200)]
    public string PublicContact { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(60)]
    public string Role { get; set; } = "User";
    
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}