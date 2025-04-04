using System.ComponentModel.DataAnnotations;
using ease_intro_api.DTOs.Member;

namespace ease_intro_api.DTOs.Meet;

public class CreateMeetDto
{
    [Required]
    [StringLength(160)]
    public string Title { get; set; } = null!;

    [Required]
    public DateTime Date { get; set; }

    [Required]
    [StringLength(260)]
    public string Location { get; set; } =  string.Empty;

    [Required]
    [Range(1, 4, ErrorMessage = "Не верно выбран статус встречи.")]
    public int StatusId { get; set; }
    
    [Range(0, int.MaxValue, ErrorMessage = "Значение должно быть больше или равно 0.")]
    public int LimitMembers { get; set; }
    
    public bool AllowedPlusOne { get; set; }
    
    public List<CreateMemberWithMeetDto>? Members { get; set; } = new();
}