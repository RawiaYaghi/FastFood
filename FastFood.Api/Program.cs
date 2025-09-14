
using FastFood.Common.Enums;
using FoodFast.BackgroundJobs;
using FoodFast.Data;
using FoodFast.Data.Models;
using FoodFast.Hubs;

//using FoodFast.Hubs;
using FoodFast.Services;
using FoodFast.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System;


//using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database Configuration - Using PostgreSQL (free)
builder.Services.AddDbContext<FoodFastDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")));


//Identity Configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<FoodFastDbContext>()
    .AddDefaultTokenProviders();

//// Redis Configuration for Pub/Sub and Caching
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
  ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")));

// SignalR for WebSocket communication
builder.Services.AddSignalR();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddAuthorization();


// Register Services
builder.Services.AddScoped<IAuthService, AuthService>();
//builder.Services.AddScoped<IOrderService, OrderService>();
//builder.Services.AddScoped<IDriverLocationService, DriverLocationService>();
//builder.Services.AddScoped<INotificationService, NotificationService>();
//builder.Services.AddScoped<IImageProcessingService, ImageProcessingService>();
builder.Services.AddSingleton<IMessageQueueService, RabbitMQService>();
//builder.Services.AddHostedService<ImageProcessingService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("FoodFastPolicy",
        builder => builder
            .WithOrigins("http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

var app = builder.Build();

// Seed roles
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = Enum.GetNames(typeof(UserRole));
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("FoodFastPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
//app.MapHub<OrderTrackingHub>("/hubs/orderTracking");
app.MapHub<RestaurantOrderHub>("/hubs/restaurantOrders");
app.MapHub<CustomerSupportHub>("/hubs/support");
//app.MapHub<DriverLocationHub>("/hubs/driverLocation");

app.Run();

