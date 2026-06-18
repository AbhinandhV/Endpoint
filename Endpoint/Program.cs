using Endpoint.Data;
using Endpoint.Services;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------
// Add services to the container
// ------------------------------
builder.Services.AddRazorPages();
builder.Services.AddControllers();

// SQLite database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=endpoint.db"));

// Windows Authentication for local use
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

// Register application services
builder.Services.AddSingleton<ActionConfigService>();
builder.Services.AddScoped<ActionHistoryService>();
builder.Services.AddSingleton<PowerShellService>();
builder.Services.AddScoped<AuditService>();

// CORS for local development
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// ------------------------------
// Ensure database is created
// ------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// ------------------------------
// Configure the HTTP pipeline
// ------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();
app.UseCors("Default");
app.UseAuthentication();
app.UseAuthorization();

// Map API and pages
app.MapControllers();
app.MapRazorPages();

// SPA fallback: serve index.html for any route not matched by API or static files
// In production, React build output goes in wwwroot/
app.MapFallbackToFile("index.html");

app.Run();