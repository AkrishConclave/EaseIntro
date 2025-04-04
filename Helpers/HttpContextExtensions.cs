using System.Security.Claims;

namespace ease_intro_api.Helpers;

public static class HttpContextExtensions
{
    public static int? GetUserId(this HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return null;

        if (int.TryParse(userIdClaim.Value, out int userId))
            return userId;

        return null;
    }
}