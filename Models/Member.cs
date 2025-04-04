using System.ComponentModel;
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
    [StringLength(80)]
    public string Name { get; set; } = null!;
    
    [Required]
    [StringLength(80)]
    public string Companion { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [StringLength(160)]
    public string Contact  { get; set; } = null!;
    
    [ForeignKey(nameof(Meet))]
    public Guid MeetGuid { get; set; }
    
    // Навигационное свойство
    public virtual Meet? Meet { get; set; }

    // Роль участника (enum)
    [Required]
    [DefaultValue(MemberRole.Guest)]
    public MemberRole Role { get; set; }
    
    // QR код участника
    [Required]
    [StringLength(160)]
    public string QrCode { get; set; } = null!;
}