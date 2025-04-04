using System.Security.Claims;
using ease_intro_api.Core.Services.Interfaces;

namespace ease_intro_api.Core.Services;

public class UserContextService : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User 
                                   ?? throw new InvalidOperationException("HttpContext or User is not available.");
    
    public bool IsAuthenticated => User.Identity?.IsAuthenticated ?? false;

    public int UserId
    {
        get
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedAccessException("User is not authenticated.");

            if (!int.TryParse(userIdClaim, out var userId))
                throw new InvalidOperationException("UserId claim is not a valid integer.");

            return userId;
        }
    }
}