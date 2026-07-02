
using SauronSheet.Application;
using SauronSheet.Application.Services;
using SauronSheet.Infrastructure;
using SauronSheet.Infrastructure.Auth;
using SauronSheet.Infrastructure.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using SauronSheet.Frontend.Services;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

// Add Sentry monitoring (Phase 6)
// Configured via Infrastructure.DependencyInjection.AddSentryMonitoring
builder.WebHost.AddSentryMonitoring(builder.Configuration);

// Register application and infrastructure services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Register progress tracking for real-time upload feedback
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IImportProgressTracker, MemoryImportProgressTracker>();

// Add Razor Pages with authorization policy
builder.Services.AddRazorPages()
    .AddRazorPagesOptions(options =>
    {
        // Require authorization for all pages by default
        options.Conventions.AuthorizeFolder("/");
        // Allow anonymous access to Auth pages
        options.Conventions.AllowAnonymousToFolder("/Auth");
        // Allow anonymous access to Index page (redirects to login/dashboard)
        options.Conventions.AllowAnonymousToPage("/Index");
        // Allow anonymous access to Error page
        options.Conventions.AllowAnonymousToPage("/Error");
    });

// Register authentication scheme so ASP.NET Core knows where to redirect
// unauthorized users (to /Auth/Login). JWT validation is handled by
// JwtCookieMiddleware — this cookie scheme is only used as the Challenge target.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/auth/login";
        options.AccessDeniedPath = "/auth/login";
        // Session-only cookie; actual auth is JWT-based via JwtCookieMiddleware
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

// Add localization services and configure the request culture pipeline.
// English is the default fallback; Spanish is the only other supported UI culture.
builder.Services.AddLocalization();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    CultureInfo[] supportedCultures = new[]
    {
        new CultureInfo("es-ES"),
        new CultureInfo("en-US")
    };

    options.DefaultRequestCulture = new RequestCulture("en-US", "en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new CookieRequestCultureProvider { CookieName = ".AspNetCore.Culture" },
        new QueryStringRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    };
});

// Add response compression (Brotli/Gzip)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Global exception middleware: catches unhandled exceptions and logs them to Sentry
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseResponseCompression();
app.UseRequestLocalization();
app.UseStaticFiles();
app.UseRouting();

// Auth middleware pipeline (Phase 1)
app.UseMiddleware<JwtCookieMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

// Language switcher endpoint: POST /api/culture?c=es|en
// Sets the culture cookie and redirects back to the referring page.
app.MapPost("/api/culture", (string? c, HttpContext context) =>
{
    if (string.IsNullOrWhiteSpace(c))
    {
        return Results.BadRequest("Culture is required.");
    }

    string cultureName = c.Trim().ToLowerInvariant() switch
    {
        "es" => "es-ES",
        "en" => "en-US",
        _ => "en-US"
    };

    RequestCulture culture = new(cultureName, cultureName);
    string cookieValue = CookieRequestCultureProvider.MakeCookieValue(culture);

    context.Response.Cookies.Append(".AspNetCore.Culture", cookieValue, new CookieOptions
    {
        Expires = DateTimeOffset.UtcNow.AddYears(1),
        HttpOnly = false,
        SameSite = SameSiteMode.Strict,
        Path = "/"
    });

    string returnUrl = context.Request.Headers.Referer.ToString();
    if (string.IsNullOrWhiteSpace(returnUrl))
    {
        returnUrl = "/";
    }

    return Results.Redirect(returnUrl);
}).AllowAnonymous();

app.MapRazorPages();

app.Run();
