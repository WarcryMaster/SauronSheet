using SauronSheet.Application;
using SauronSheet.Infrastructure;
using SauronSheet.Infrastructure.Auth;


var builder = WebApplication.CreateBuilder(args);

// Register application and infrastructure services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add Razor Pages
builder.Services.AddRazorPages();

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

// Security headers middleware
app.UseMiddleware<SauronSheet.Infrastructure.Middleware.SecurityHeadersMiddleware>();

// Auth middleware pipeline (Phase 1)
app.UseMiddleware<JwtCookieMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
