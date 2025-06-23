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

// -- Log du d�marrage du processus de construction de l'h�te -- 
Log.Information("D�marrage de la construction de l'h�te pour mon minimal API.");
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

// -- Log apr�s l'ajout des services (avant la construction de l'app) -- 
Log.Information("Services configur�s. Debut construction de l'application...");

var app = builder.Build();

// -- Log apr�s la construction de l'application -- 
Log.Information("Application construite.");

// -- Configure the HTTP request pipeline. -- 
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.Logger.LogInformation("Environnement de d�veloppement d�tect�. Swagger UI activ�.");
}

app.UseHttpsRedirection();

#region -- Serilog --
// --- Utilisation de Serilog pour les requ�tes HTTP ---
app.UseSerilogRequestLogging();
app.Logger.LogInformation("Middleware SerilogRequestLogging ajout� au pipeline.");

// -- Enregistrement des �v�nements du cycle de vie de l'application -- 
app.Lifetime.ApplicationStarted.Register(() =>
{
    app.Logger.LogInformation("Application D�marr�e : L'application est maintenant en cours d'ex�cution et pr�te � recevoir des requ�tes.");
    app.Logger.LogInformation($"Listening on: {string.Join(", ", app.Urls)}");
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    app.Logger.LogWarning("Application Arr�t en cours : L'application re�oit un signal d'arr�t.");
});

app.Lifetime.ApplicationStopped.Register(() =>
{
    app.Logger.LogCritical("Application Arr�t�e : L'application s'est compl�tement arr�t�e.");
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

