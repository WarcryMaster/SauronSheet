
using SauronSheet.Application;
using SauronSheet.Infrastructure;
using SauronSheet.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add Sentry monitoring (Phase 6)
// Configured via Infrastructure.DependencyInjection.AddSentryMonitoring
builder.WebHost.AddSentryMonitoring(builder.Configuration);

// Register application and infrastructure services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

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
}

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseStaticFiles();
app.UseRouting();

// Auth middleware pipeline (Phase 1)
app.UseMiddleware<JwtCookieMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
