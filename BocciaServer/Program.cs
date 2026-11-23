using System.Text.Json.Serialization;
using BocciaServer.Data;
using BocciaServer.Hubs;
using BocciaServer.Infrastructure;
using BocciaServer.Services;
using BocciaShared.Hubs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("BocciaDb")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexión 'ConnectionStrings:BocciaDb'. " +
        "Configurala con: dotnet user-secrets set \"ConnectionStrings:BocciaDb\" \"<cadena>\"");

builder.Services.AddDbContext<BocciaDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<MatchService>();
builder.Services.AddScoped<IMatchNotifier, SignalRMatchNotifier>();

// Los enums viajan como texto ("Red", "InProgress"...) tanto en la API como en SignalR.
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSignalR()
    .AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Sin redirección a HTTPS: en la red local los dispositivos se conectan por HTTP.
app.MapControllers();
app.MapHub<ScoreboardHub>(ScoreboardHubContract.Path);

app.Run();

// Permite referenciar el punto de entrada desde pruebas de integración.
public partial class Program;
