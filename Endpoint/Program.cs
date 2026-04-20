using Endpoint.Services;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------
// Add services to the container
// ------------------------------
builder.Services.AddRazorPages();
builder.Services.AddControllers();

// Register application services
builder.Services.AddSingleton<ActionConfigService>();
builder.Services.AddSingleton<ActionHistoryService>();
builder.Services.AddSingleton<PowerShellService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

var app = builder.Build();

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

// ✅ Order is VERY important
app.UseRouting();          // 1. Routing first
app.UseCors("AllowAll");   // 2. Then CORS
app.UseAuthorization();    // 3. Then Auth

// ------------------------------
// Map endpoints
// ------------------------------
app.MapControllers();      // API
app.MapRazorPages();       // UI (optional)

app.Run();