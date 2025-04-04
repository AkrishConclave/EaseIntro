using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ease_intro_api.Models;

public class Member
{
    public enum MemberRole
    {
        Main,
        Admin,
        Staff,
        Guest
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [StringLength(60)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(60)]
    public string Companion { get; set; } = string.Empty;
    
    [Required]
    [StringLength(80)]
    public string Contact  { get; set; } = string.Empty;
    
    [ForeignKey(nameof(Meet))]
    public Guid MeetGuid { get; set; }
    
    // Навигационное свойство
    public virtual Meet? Meet { get; set; }

    // Роль участника (enum)
    [Required]
    public MemberRole Role { get; set; } = MemberRole.Guest;
}