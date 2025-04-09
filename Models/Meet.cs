using System.ComponentModel;
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

    // todo Написать валидатор что бы не ввели прошлую дату
    [Required]
    public DateTime Date { get; set; }

    [Required]
    [StringLength(260)]
    public string Location { get; set; } = string.Empty;

    // Внешний ключ
    [ForeignKey("Status")]
    public int StatusId { get; set; }

    // Лимит 0 - бесконечно
    [DefaultValue(0)]
    public int LimitMembers { get; set; }

    [DefaultValue(false)]
    public bool AllowedPlusOne { get; set; } 
    
    [ForeignKey("Owner")]
    public int OwnerId { get; set; }

    public virtual User Owner { get; set; } = null!;

    // Навигационное свойство
    [Required]
    public virtual MeetStatus? Status { get; set; }
    
    public virtual ICollection<Member> Members { get; set; } = new List<Member>();
}