using System.ComponentModel.DataAnnotations;

namespace ease_intro_api.DTOs.Member;

public class CreateMemberWithMeetDto
{
    [Required]
    [StringLength(60)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(60)]
    public string Companion { get; set; } = string.Empty;
    
    [Required]
    [StringLength(80)]
    public string Contact { get; set; } = string.Empty;
    
    [EnumDataType(typeof(Models.Member.MemberRole))]
    public Models.Member.MemberRole? Role { get; set; }
}