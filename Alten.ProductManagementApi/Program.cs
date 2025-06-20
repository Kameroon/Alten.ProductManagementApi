using Microsoft.AspNetCore.Authentication.JwtBearer; 
using Microsoft.IdentityModel.Tokens; 
using Npgsql;
using Alten.ProductManagementApi.Extensions;
using System.Data;
using System.Security.Claims; 
using System.Text; 

var builder = WebApplication.CreateBuilder(args);

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
    // You can define policies here, e.g., options.AddPolicy("AdminOnly", policy => policy.RequireClaim(ClaimTypes.Email, "admin@admin.com"));
    // For this exercise, we are doing the claim check manually in the endpoint for simplicity as requested.
});

var app = builder.Build();

// -- Configure the HTTP request pipeline. -- 
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// -- IMPORTANT: Authentication avant Authorization -- 
app.UseAuthentication();
app.UseAuthorization();

// -- Map Endpoints using extension methods for cleaner Program.cs -- 
app.MapAuthenticationEndpoints();
app.MapProductEndpoints();
app.MapCartEndpoints();
app.MapWishlistEndpoints();

app.Run();

