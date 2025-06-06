using System.ComponentModel.DataAnnotations;

namespace ease_intro_api.DTOs.Meet;

public class UpdateMeetDto
{
    [StringLength(160)]
    public string Title { get; set; } = null!;
    
    public DateTime Date { get; set; }

    [StringLength(260)]
    public string Location { get; set; } = string.Empty;
    
    [Range(1, 4, ErrorMessage = "Не верно выбран статус встречи.")]
    public int StatusId { get; set; }
    
    [Range(0, int.MaxValue, ErrorMessage = "Значение должно быть больше или равно 0.")]
    public int LimitMembers { get; set; }
    
    public bool AllowedPlusOne { get; set; }
}