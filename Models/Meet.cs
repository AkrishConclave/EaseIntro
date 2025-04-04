using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ease_intro_api.Models;

public class Meet
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Uid { get; set; }

    [Required]
    [StringLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public DateTime Date { get; set; }

    [Required]
    [StringLength(260)]
    public string? Location { get; set; }

    [ForeignKey("Status")]
    public int StatusId { get; set; }  // Внешний ключ

    [Required]
    public virtual MeetStatus? Status { get; set; }  // Навигационное свойство
    
    public virtual ICollection<Member> Members { get; set; } = new List<Member>();
}