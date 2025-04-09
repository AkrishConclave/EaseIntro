using System.ComponentModel.DataAnnotations;

namespace ease_intro_api.DTOs.Meet;

public class MeetUpdateDto
{
    [StringLength(160)]
    public string Title { get; set; } = string.Empty;
    
    public DateTime Date { get; set; }

    [Required]
    [StringLength(260)]
    public string Location { get; set; } = string.Empty;

    [Required]
    [Range(1, 4)]
    public int StatusId { get; set; }
    
    public int LimitMembers { get; set; }
    
    public bool AllowedPlusOne { get; set; }
}