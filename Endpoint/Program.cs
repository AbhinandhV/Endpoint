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

// Authentication: Use API Key on Linux/cloud, Windows Auth on Windows
var useApiKey = !OperatingSystem.IsWindows() ||
    !string.IsNullOrEmpty(builder.Configuration["ApiKey"]);

if (useApiKey)
{
    // Simple API Key authentication for cloud hosting
    builder.Services.AddAuthentication("ApiKey")
        .AddScheme<ApiKeyAuthOptions, ApiKeyAuthHandler>("ApiKey", null);
}
else
{
    // Windows Authentication (Active Directory)
    builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
        .AddNegotiate();
}
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

// Register application services
builder.Services.AddSingleton<ActionConfigService>();
builder.Services.AddScoped<ActionHistoryService>();
builder.Services.AddSingleton<PowerShellService>();
builder.Services.AddScoped<AuditService>();

// CORS — restrict in production, allow all in development
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins("http://localhost:3000")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            // In production, allow Netlify frontend and any configured origins
            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                ?? Array.Empty<string>();
            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            }
            else
            {
                // Fallback: allow any origin for testing (no credentials)
                policy.SetIsOriginAllowed(_ => true)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            }
        }
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