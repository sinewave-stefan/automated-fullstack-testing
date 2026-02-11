using Game.Server.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add SignalR services
builder.Services.AddSignalR();

// Add CORS to allow web and native clients to connect
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5000", "http://localhost:5001", "https://localhost:5001")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Enable CORS
app.UseCors();

// Map the SignalR hub
app.MapHub<GameHub>("/gamehub");

app.MapGet("/", () => "Game Server is running. Connect to /gamehub for realtime communication.");

app.Run();

// Make Program class accessible for testing
public partial class Program { }
