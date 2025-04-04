using System.Security.Claims;

namespace ease_intro_api.Core.Services.Interfaces;

public interface IUserContextService
{
    int UserId { get; }
    ClaimsPrincipal User { get; }
    bool IsAuthenticated { get; }
}