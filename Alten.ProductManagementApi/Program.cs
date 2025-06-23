using Microsoft.AspNetCore.Authentication.JwtBearer; 
using Microsoft.IdentityModel.Tokens; 
using Npgsql;
using Alten.ProductManagementApi.Extensions;
using System.Data;
using System.Security.Claims; 
using System.Text;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

#region -- Configuration Serilog --
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console() 
    .CreateLogger();

builder.Host.UseSerilog();

// -- Log du démarrage du processus de construction de l'hôte -- 
Log.Information("Démarrage de la construction de l'hôte pour mon minimal API.");
#endregion

// -- Add services to the container. -- 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// -- Configure PostgreSQL connection for Dapper -- 
builder.Services.AddSingleton<IDbConnection>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    return new NpgsqlConnection(connectionString);
});

// -- Add Application Services (Repositories and Services) -- 
builder.Services.AddApplicationServices();

// -- Add JWT Authentication -- 
builder.Services.AddJwtAuthentication(builder.Configuration);

// -- Add Authorization policies (if needed, though admin check is manual for simplicity here) -- 
builder.Services.AddAuthorization(options =>
{
    
});

// -- Log après l'ajout des services (avant la construction de l'app) -- 
Log.Information("Services configurés. Debut construction de l'application...");

var app = builder.Build();

// -- Log après la construction de l'application -- 
Log.Information("Application construite.");

// -- Configure the HTTP request pipeline. -- 
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.Logger.LogInformation("Environnement de développement détecté. Swagger UI activé.");
}

app.UseHttpsRedirection();

#region -- Serilog --
// --- Utilisation de Serilog pour les requêtes HTTP ---
app.UseSerilogRequestLogging();
app.Logger.LogInformation("Middleware SerilogRequestLogging ajouté au pipeline.");

// -- Enregistrement des événements du cycle de vie de l'application -- 
app.Lifetime.ApplicationStarted.Register(() =>
{
    app.Logger.LogInformation("Application Démarrée : L'application est maintenant en cours d'exécution et prête à recevoir des requêtes.");
    app.Logger.LogInformation($"Listening on: {string.Join(", ", app.Urls)}");
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    app.Logger.LogWarning("Application Arrêt en cours : L'application reçoit un signal d'arrêt.");
});

app.Lifetime.ApplicationStopped.Register(() =>
{
    app.Logger.LogCritical("Application Arrêtée : L'application s'est complètement arrêtée.");
    Log.CloseAndFlush();
});
#endregion

// -- IMPORTANT: Authentication avant Authorization -- 
app.UseAuthentication();
app.UseAuthorization();

// -- Map Endpoints using extension methods for cleaner Program.cs -- 
app.MapAuthenticationEndpoints();
app.MapProductEndpoints();
app.MapCartEndpoints();
app.MapWishlistEndpoints();

app.Run();

