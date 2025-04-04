using System.ComponentModel.DataAnnotations;

namespace ease_intro_api.DTOs.Member;

public class UpdateMemberDto
{
    [StringLength(80)]
    public string Name { get; set; } = null!;
    
    [StringLength(80)]
    public string Companion { get; set; } = string.Empty;
    
    [EmailAddress]
    [StringLength(160)]
    public string Contact { get; set; } = null!;
    
    [EnumDataType(typeof(Models.Member.MemberRole))]
    public int? Role { get; set; }
}