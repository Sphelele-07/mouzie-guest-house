using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Mouzie.Data.DataAccess;
using Mouzie.Data.Repositories.Implementations;
using Mouzie.Data.Repositories.Interfaces;
using Mouzie.UI.Services;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════
// CONFIGURATION — SMTP settings mapped from EmailSettings block
// ═══════════════════════════════════════════════════════════════
var emailSection = builder.Configuration.GetSection("EmailSettings");
builder.Services.Configure<SmtpOptions>(options =>
{
    options.Host = emailSection["SmtpHost"] ?? "smtp.gmail.com";
    options.Port = int.TryParse(emailSection["SmtpPort"], out var p) ? p : 587;
    options.UseStartTls = true;
    options.Username = emailSection["SmtpUser"] ?? "";
    options.Password = emailSection["SmtpPass"] ?? "";
    options.FromName = emailSection["SenderName"] ?? "Mouzie Guest House";
    options.FromEmail = emailSection["SenderEmail"] ?? "";
});
builder.Services.Configure<SiteOptions>(builder.Configuration.GetSection("Site"));

// ═══════════════════════════════════════════════════════════════
// DATA LAYER
// ═══════════════════════════════════════════════════════════════
builder.Services.AddScoped<ISqlDataAccess, SqlDataAccess>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISiteSettingRepository, SiteSettingRepository>();
builder.Services.AddScoped<IBookingCodeGenerator, BookingCodeGenerator>();
builder.Services.AddScoped<IDamageRepository, DamageRepository>();

// ═══════════════════════════════════════════════════════════════
// UI SERVICES
// ═══════════════════════════════════════════════════════════════
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();

// ═══════════════════════════════════════════════════════════════
// MVC + ANTI-FORGERY (CSRF) — all POSTs require token
// ═══════════════════════════════════════════════════════════════
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());

    // Hide debug details from end users in production
    if (!builder.Environment.IsDevelopment())
    {
        options.Filters.Add(new RequireHttpsAttribute());
    }
});

// ═══════════════════════════════════════════════════════════════
// SESSION — secure cookies
// ═══════════════════════════════════════════════════════════════
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromHours(8);
    o.Cookie.Name = ".Mouzie.Session";
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.Cookie.SameSite = SameSiteMode.Lax;     // CSRF protection for cross-site
});

// ═══════════════════════════════════════════════════════════════
// RATE LIMITING — brute-force protection on login and booking
// ═══════════════════════════════════════════════════════════════
builder.Services.AddRateLimiter(options =>
{
    // Policies are applied via [EnableRateLimiting("auth")] attribute
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));

    options.AddPolicy("booking", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));

    // Reject with 429 (Too Many Requests) instead of crashing
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ═══════════════════════════════════════════════════════════════
// HSTS + HTTPS enforcement
// ═══════════════════════════════════════════════════════════════
builder.Services.AddHsts(o =>
{
    o.Preload = true;
    o.IncludeSubDomains = true;
    o.MaxAge = TimeSpan.FromDays(365);
});

// ═══════════════════════════════════════════════════════════════
// HEALTH CHECKS (optional — useful for MonsterASP uptime monitor)
// ═══════════════════════════════════════════════════════════════
builder.Services.AddHealthChecks();

// ═══════════════════════════════════════════════════════════════
// SECURITY HEADERS — applied via middleware below
// ═══════════════════════════════════════════════════════════════

var app = builder.Build();

// ═══════════════════════════════════════════════════════════════
// PIPELINE — order matters!
// ═══════════════════════════════════════════════════════════════

// 1. Error handling (production) — never leak stack traces
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();                             // HTTP Strict Transport Security
}
else
{
    app.UseDeveloperExceptionPage();
}

// 2. Security headers middleware — MUST be early
app.Use(async (context, next) =>
{
    // Block MIME-type sniffing
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";

    // Block the page from being embedded in an iframe (clickjacking)
    context.Response.Headers["X-Frame-Options"] = "DENY";

    // Referrer policy — no information leak to third parties
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

    // Permissions policy — disable browser features you don't use
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

    // Content Security Policy — blocks inline scripts (XSS hardening)
    // Note: relax the "script-src" if you use inline <script> tags
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' data:; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'";

    await next();
});

// 3. HTTPS redirect (production)
app.UseHttpsRedirection();

// 4. Static files
app.UseStaticFiles();

// 5. Routing (before rate limiter/session — required order)
app.UseRouting();

// 6. Rate limiter — applies to policies used in controllers
app.UseRateLimiter();

// 7. Session — AFTER routing, BEFORE authorization
app.UseSession();

// 8. Authorization
app.UseAuthorization();

// 9. Health check endpoint
app.MapHealthChecks("/health");

// 10. Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();