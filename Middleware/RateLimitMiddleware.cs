using System.Collections.Concurrent;
using System.Net;

namespace ease_intro_api.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;
    
    // Словарь для хранения количества запросов по IP
    private static readonly ConcurrentDictionary<string, RateLimitInfo> _requestCounts = new();
    
    // Лимиты: максимум 5 запросов за 1 минуту для auth endpoints
    private const int MaxRequests = 5;
    private static readonly TimeSpan TimeWindow = TimeSpan.FromMinutes(1);

    public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Применяем rate limiting только к auth endpoints
        if (context.Request.Path.StartsWithSegments("/api/auth"))
        {
            var clientIp = GetClientIp(context);
            var now = DateTime.UtcNow;

            var rateLimitInfo = _requestCounts.AddOrUpdate(
                clientIp,
                new RateLimitInfo { Count = 1, ResetTime = now.Add(TimeWindow) },
                (key, existing) =>
                {
                    // Если окно времени истекло, сбрасываем счетчик
                    if (now > existing.ResetTime)
                    {
                        return new RateLimitInfo { Count = 1, ResetTime = now.Add(TimeWindow) };
                    }
                    
                    // Увеличиваем счетчик
                    existing.Count++;
                    return existing;
                });

            // Проверяем лимит
            if (rateLimitInfo.Count > MaxRequests)
            {
                _logger.LogWarning("Rate limit exceeded for IP: {Ip}", clientIp);
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"error\":\"Слишком много запросов. Попробуйте позже.\"}");
                return;
            }

            // Добавляем заголовки с информацией о лимите
            context.Response.Headers["X-RateLimit-Limit"] = MaxRequests.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, MaxRequests - rateLimitInfo.Count).ToString();
            context.Response.Headers["X-RateLimit-Reset"] = ((DateTimeOffset)rateLimitInfo.ResetTime).ToUnixTimeSeconds().ToString();
        }

        await _next(context);
    }

    private static string GetClientIp(HttpContext context)
    {
        // Проверяем заголовки прокси
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private class RateLimitInfo
    {
        public int Count { get; set; }
        public DateTime ResetTime { get; set; }
    }
}


