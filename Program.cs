using HeThongThiDQ.Common;
using HeThongThiDQ.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

// Response compression (Brotli + Gzip)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

// EF Core with retry on transient failures
builder.Services.AddDbContext<ELEARNINGEntities>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnectionString"),
        sql => sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null)));

// Distributed cache (Redis) — dùng chung giữa tất cả IIS instances
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "HPDQ:";
});

// IConnectionMultiplexer — cho ZSET, INCR và các lệnh Redis nâng cao
builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(
    StackExchange.Redis.ConnectionMultiplexer.Connect(
        builder.Configuration.GetConnectionString("Redis")!));

// Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index";
        options.LogoutPath = "/Login/Logout";
        options.AccessDeniedPath = "/Login/Index";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(360);
        options.SlidingExpiration = true;
        options.Cookie.Name = ".HPDQ.Auth";
    });

// Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(360);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// RabbitMQ — publisher singleton, consumer background service
builder.Services.AddSingleton<HeThongThiDQ.Services.IExamQueuePublisher,
                               HeThongThiDQ.Services.RabbitMqPublisher>();
builder.Services.AddHostedService<HeThongThiDQ.Services.ExamSubmitConsumer>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<MyAuthentication>();
builder.Services.AddScoped<HeThongThiDQ.Controllers.HomeController>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();  // trust nginx từ bất kỳ IP nào
    options.KnownProxies.Clear();
});

// Persist Data Protection keys vào DB — chia sẻ key giữa tất cả IIS worker processes
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ELEARNINGEntities>()
    .SetApplicationName("HeThongThiDQ");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseResponseCompression(); // Phải trước UseStaticFiles
app.UseForwardedHeaders();      // Phải gọi trước
// app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseMiddleware<HeThongThiDQ.Middleware.SessionGuardMiddleware>();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();
