//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.
//// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();

using Microsoft.AspNetCore.Authentication.JwtBearer; // Required for JwtBearerDefaults
using Microsoft.IdentityModel.Tokens; // Required for SecurityTokenDescriptor
using Npgsql;
using Alten.ProductManagementApi.Extensions; // Pour tes méthodes d'extension
using System.Data;
using System.Security.Claims; // Required for ClaimTypes
using System.Text; // Required for Encoding

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure PostgreSQL connection for Dapper
builder.Services.AddSingleton<IDbConnection>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    return new NpgsqlConnection(connectionString);
});

// Add Application Services (Repositories and Services)
builder.Services.AddApplicationServices();

// Add JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Add Authorization policies (if needed, though admin check is manual for simplicity here)
builder.Services.AddAuthorization(options =>
{
    // You can define policies here, e.g., options.AddPolicy("AdminOnly", policy => policy.RequireClaim(ClaimTypes.Email, "admin@admin.com"));
    // For this exercise, we are doing the claim check manually in the endpoint for simplicity as requested.
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// IMPORTANT: Authentication and Authorization middleware must be in this order
app.UseAuthentication();
app.UseAuthorization();

// Map Endpoints using extension methods for cleaner Program.cs
app.MapAuthenticationEndpoints();
app.MapProductEndpoints();
app.MapCartEndpoints();
app.MapWishlistEndpoints();

app.Run();

//var summaries = new[]
//{
//    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
//};

//app.MapGet("/weatherforecast", () =>
//{
//    var forecast = Enumerable.Range(1, 5).Select(index =>
//        new WeatherForecast
//        (
//            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//            Random.Shared.Next(-20, 55),
//            summaries[Random.Shared.Next(summaries.Length)]
//        ))
//        .ToArray();
//    return forecast;
//})
//.WithName("GetWeatherForecast")
//.WithOpenApi();

//app.Run();

//internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
//{
//    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
//}
