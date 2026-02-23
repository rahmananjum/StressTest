using Microsoft.EntityFrameworkCore;
using StressTest.Core.Data;
using StressTest.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// SQLite database - placed in the app's data folder
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "Data", "stresstest.db");
builder.Services.AddDbContext<StressTestDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// CSV data directory (same Data folder as the DB)
var csvDir = Path.Combine(builder.Environment.ContentRootPath, "Data");
builder.Services.AddSingleton<ICsvDataService>(_ => new CsvDataService(csvDir));

builder.Services.AddSingleton<IStressTestCalculator, StressTestCalculator>();
builder.Services.AddScoped<IStressTestRunService, StressTestRunService>();

// ── App ───────────────────────────────────────────────────────────────────────

var app = builder.Build();

// Auto-create / migrate the SQLite database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StressTestDbContext>();
    db.Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
