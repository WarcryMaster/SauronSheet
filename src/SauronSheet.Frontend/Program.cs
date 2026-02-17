using SauronSheet.Application;
using SauronSheet.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Register application and infrastructure services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add Razor Pages
builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Authentication and Authorization middleware (Phase 1)
// app.UseAuthentication();
// app.UseAuthorization();

app.MapRazorPages();

app.Run();
