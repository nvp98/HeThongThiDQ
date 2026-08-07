using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Security.Claims;

namespace HeThongThiDQ.Middleware
{
    public class SessionGuardMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _mux;

        public SessionGuardMiddleware(RequestDelegate next, IDistributedCache cache, IConnectionMultiplexer mux)
        {
            _next  = next;
            _cache = cache;
            _mux   = mux;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Bỏ qua trang login để tránh redirect loop
            if (context.Request.Path.StartsWithSegments("/Login"))
            {
                await _next(context);
                return;
            }

            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var idNVStr     = context.User.FindFirstValue("NV_ID");
                var cookieToken = context.User.FindFirstValue("NV_ST");

                if (int.TryParse(idNVStr, out int idNV) && idNV > 0)
                {
                    // Kiểm tra single-device (chỉ khi cookie có token NV_ST)
                    if (!string.IsNullOrEmpty(cookieToken))
                    {
                        try
                        {
                            var redisToken = await _cache.GetStringAsync($"user:session:{idNV}");
                            if (redisToken != null && redisToken != cookieToken)
                            {
                                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                                context.Response.Redirect("/Login/Index?kicked=1");
                                return;
                            }
                        }
                        catch { }
                    }

                    // Cập nhật trạng thái online (ZADD — O(log N), rất nhẹ)
                    try
                    {
                        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        await _mux.GetDatabase()
                            .SortedSetAddAsync("HPDQ:online:users", idNV.ToString(), nowMs);
                    }
                    catch { }
                }
            }

            await _next(context);
        }
    }
}
