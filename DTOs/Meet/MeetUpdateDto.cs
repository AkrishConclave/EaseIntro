using System.ComponentModel.DataAnnotations;

namespace ease_intro_api.DTOs.Meet;

public class MeetUpdateDto
{
    [Required]
    [StringLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public DateTime Date { get; set; }

    [StringLength(260)]
    public string? Location { get; set; }

    [Required]
    [Range(1, 4)]
    public int StatusId { get; set; }
}