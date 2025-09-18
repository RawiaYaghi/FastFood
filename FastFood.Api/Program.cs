
using FastFood.Api.Hubs;
using FastFood.Api.Services;
using FastFood.Api.Services.Interfaces;
using FastFood.Common.Enums;
using FoodFast.BackgroundJobs;
using FoodFast.Data;
using FoodFast.Data.Models;
using FoodFast.Services;
using FoodFast.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromSeconds(30);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
});





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


        // Allow JWT in query string for SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();


builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FoodFast API", Version = "v1" });

    // JWT Authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your valid JWT token."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

});


// Register Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddTransient<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderNotificationService, OrderNotificationService>();
builder.Services.AddScoped<IChatService, ChatService>();

//builder.Services.AddScoped<IDriverLocationService, DriverLocationService>();
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
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FoodFast API V1");
        c.RoutePrefix = string.Empty; // Optional: Swagger at root
    });
}

app.UseHttpsRedirection();
app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowFrontend");
app.Use(async (context, next) =>
{
    Console.WriteLine($"Request Path: {context.Request.Path}");
    await next();
});
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/chatHub");


//app.MapHub<OrderTrackingHub>("/hubs/orderTracking");
//app.MapHub<RestaurantOrderHub>("/hubs/restaurantOrders");
//app.MapHub<CustomerSupportHub>("/hubs/support");
//app.MapHub<DriverLocationHub>("/hubs/driverLocation");

app.Run();

