using System.ComponentModel.DataAnnotations;

namespace ease_intro_api.DTOs.Member;

public class UpdateMemberDto
{
    [StringLength(60)]
    public string? Name { get; set; }
    
    [StringLength(60)]
    public string? Companion { get; set; }
    
    [StringLength(80)]
    public string? Contact { get; set; }
    
    [EnumDataType(typeof(Models.Member.MemberRole))]
    public int? Role { get; set; }
}