
using Persistence;
using Application;
using Infrastructure;
using Serilog;
using Serilog.Events;
using PetSocialAPI.Middlewares;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

var logFilePath = builder.Configuration["Logging:FilePath"] ?? "Logs/PetSocialLog/requests.txt";
var fullLogPath = Path.GetFullPath(logFilePath);
Directory.CreateDirectory(Path.GetDirectoryName(fullLogPath)!);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error)
    .WriteTo.Console()
    .WriteTo.File(fullLogPath, rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:PermitLimit", 100);
        opt.Window = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("RateLimiting:WindowMinutes", 1));
    });
});
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer("Bearer", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        ValidateLifetime = false,
    };

});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDevClient", builder =>
    {
        builder
            .WithOrigins("http://localhost:5173", "https://robert2810-guc.github.io") // Your React dev server
            .AllowAnyHeader()
            .AllowAnyMethod();
            //.AllowCredentials(); // Optional: only if you use cookies
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Input your Bearer token to access this API"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] {}
        }
    });
}); bool enableRequestLogging = builder.Configuration.GetValue<bool>("EnableRequestLogging");


var app = builder.Build();
if (enableRequestLogging)
{
    app.UseMiddleware<RequestTimingMiddleware>();
}


app.UseSwagger();
app.UseReDoc(options =>
{
    options.DocumentTitle = "PetSocial API Documentation";
    options.SpecUrl("/swagger/v1/swagger.json");
    options.RoutePrefix = "docs";
});
app.UseSwaggerUI();
app.UseCors("AllowDevClient");

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
