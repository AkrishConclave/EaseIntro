namespace ease_intro_api.DTOs.Meet;

public class MeetResponseDto
{
    public Guid Uid { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? Location { get; set; }
    public MeetStatusDto? Status { get; set; }
}