using FlashcardApi.Data;
using FlashcardApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure structured logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configure log levels for better visibility
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("FlashcardApi.Services.ApkgParserService", LogLevel.Debug);
builder.Logging.AddFilter("FlashcardApi.Controllers.DecksController", LogLevel.Information);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure database context with SQL Server
builder.Services.AddDbContext<FlashcardDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register application services
builder.Services.AddScoped<IFsrsService, FsrsService>();
builder.Services.AddScoped<IApkgParserService, ApkgParserService>();
builder.Services.AddScoped<IDeckService, DeckService>();
builder.Services.AddScoped<ICardService, CardService>();
builder.Services.AddScoped<IStatsService, StatsService>();
builder.Services.AddScoped<ILLMService, LLMService>();

// Register HttpClient for LLM service with SSL certificate handling
builder.Services.AddHttpClient<ILLMService, LLMService>(client =>
{
    // Configure HttpClient settings
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    
    // Only ignore SSL certificate validation in development
    if (builder.Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback = 
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }
    
    return handler;
});

// Configure CORS to allow React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Apply database migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FlashcardDbContext>();
    try
    {
        // Wait for database to be ready and apply migrations
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable static file serving for media files
app.UseStaticFiles();

app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

app.Run();