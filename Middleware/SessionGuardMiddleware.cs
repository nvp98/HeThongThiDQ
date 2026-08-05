using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;

namespace HeThongThiDQ.Middleware
{
    public class SessionGuardMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionGuardMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IDistributedCache cache)
        {
            // Bỏ qua trang login để tránh redirect loop
            if (context.Request.Path.StartsWithSegments("/Login"))
            {
                await _next(context);
                return;
            }

            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var idNVStr      = context.User.FindFirstValue("NV_ID");
                var cookieToken  = context.User.FindFirstValue("NV_ST");

                // Chỉ kiểm tra khi cookie có token (đăng nhập sau khi tính năng được bật)
                if (int.TryParse(idNVStr, out int idNV) && idNV > 0 && !string.IsNullOrEmpty(cookieToken))
                {
                    try
                    {
                        var redisToken = await cache.GetStringAsync($"user:session:{idNV}");

                        // Redis có token nhưng không khớp → thiết bị khác đã đăng nhập
                        if (redisToken != null && redisToken != cookieToken)
                        {
                            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                            context.Response.Redirect("/Login/Index?kicked=1");
                            return;
                        }
                    }
                    catch
                    {
                        // Redis down — cho qua, không block người dùng
                    }
                }
            }

            await _next(context);
        }
    }
}
