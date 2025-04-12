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
    
    /**
     * Если надо управлять валидацией роли не на уровне <i>.net core</i>, то следует
     * убрать эту аннотацию, а в контроллере установить метод проверки из
     * <b>`MemberService.CheckRoleMembers()`</b>, тогда мы сможем вернуть ошибку
     * <b>`BadRequest()`</b> с нужным текстом.
     */
    [EnumDataType(typeof(Models.Member.MemberRole))]
    public Models.Member.MemberRole? Role { get; set; }
}