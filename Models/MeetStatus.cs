using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ease_intro_api.Models;

public class MeetStatus
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(60)]
    public string Title { get; set; } = string.Empty;

    [StringLength(120)]
    public string? Description { get; set; }

    // Навигационное свойство для встреч с этим статусом
    public virtual ICollection<Meet> Meets { get; set; } = new List<Meet>();
}