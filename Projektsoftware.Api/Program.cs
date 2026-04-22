using Projektsoftware.Api.Services;
using Projektsoftware.Api.Components;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ApiDatabaseService>();
builder.Services.AddSingleton<ApiEmailService>();

// Blazor Server-Side Rendering
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddScoped<SessionService>();

// CORS erlauben
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Forwarded Headers für Nginx-SSL-Proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownProxies.Clear();
    options.KnownIPNetworks.Clear();
});

var app = builder.Build();

// Forwarded Headers als erstes in der Pipeline
app.UseForwardedHeaders();

// Datenbank-Tabellen automatisch erstellen
var dbService = app.Services.GetRequiredService<ApiDatabaseService>();
try
{
    await dbService.InitializeDatabaseAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: Database initialization failed: {ex.Message}");
    // App läuft weiter, auch wenn DB-Init fehlschlägt
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// HTTPS-Umleitung erzwingen
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors();
app.UseAntiforgery();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.Run();
