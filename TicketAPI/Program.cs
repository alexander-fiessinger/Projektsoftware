using TicketAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Register DatabaseService
builder.Services.AddScoped<DatabaseService>();

// Configure CORS
var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() 
    ?? new[] { "*" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebsite", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });

    // Alternative: Alle Origins erlauben (nur für Entwicklung!)
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("AllowAll"); // Für Entwicklung
}
else
{
    app.UseCors("AllowWebsite"); // Für Produktion
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Health Check Endpoint
app.MapGet("/health", () => Results.Ok(new 
{ 
    status = "healthy", 
    timestamp = DateTime.Now,
    service = "Ticket API"
}))
.WithName("HealthCheck");

Console.WriteLine("🚀 Ticket API gestartet!");
Console.WriteLine("📋 Swagger UI: https://localhost:{port}/swagger");
Console.WriteLine("🔗 API Endpoint: https://localhost:{port}/api/tickets");
Console.WriteLine("💚 Health Check: https://localhost:{port}/health");

app.Run();
