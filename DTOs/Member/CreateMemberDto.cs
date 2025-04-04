using System.ComponentModel.DataAnnotations;

namespace ease_intro_api.DTOs.Member;

public class CreateMemberDto
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
    
    [Required]
    public Guid MeetUid { get; set; }
    
    [EnumDataType(typeof(Models.Member.MemberRole))]
    public Models.Member.MemberRole Role { get; set; } = Models.Member.MemberRole.Guest;
    
    [StringLength(160)]
    public string QrCode { get; set; } = string.Empty;
}