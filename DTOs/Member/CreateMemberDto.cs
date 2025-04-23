using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ease_intro_api.DTOs.Member;

public class CreateMemberDto
{
    [Required]
    [StringLength(80)]
    public string Name { get; set; } = null!;
    
    [Required]
    [StringLength(80)]
    public string Companion { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [StringLength(160)]
    public string Contact { get; set; } = null!;
    
    [Required]
    [JsonPropertyName("meetUid")]
    public Guid MeetUid { get; set; }
}