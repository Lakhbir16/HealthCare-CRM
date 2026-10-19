using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using HealthcareCRM.API.Middleware;
using HealthcareCRM.API.Services;
using HealthcareCRM.Application.Common;
using HealthcareCRM.Application.Interfaces;
using HealthcareCRM.Infrastructure;
using HealthcareCRM.Infrastructure.Persistence;
using HealthcareCRM.Infrastructure.Seed;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    // 1. Dependency Injection: Application & Infrastructure
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // 2. Controllers & JSON Serialization
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

    // 3. JWT Authentication
    var jwtSecret = builder.Configuration["Jwt:SecretKey"] ?? "HealthcareCRM_ProductionGrade_SuperSecureKey_2026_HIPAACompliant_TokenSecretKey!";
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "HealthcareCRM.API";
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "HealthcareCRM.Client";

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // set to true in production with SSL
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

    // 4. CORS
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
        ?? new[] { "http://localhost:4200", "http://127.0.0.1:4200" };

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("HealthcareCrmCorsPolicy", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // 5. Swagger / OpenAPI Documentation
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "US Healthcare Management CRM API",
            Version = "v1",
            Description = "Production-oriented REST API for US Healthcare Management CRM (.NET 8, Clean Architecture, SQL Server, JWT, HIPAA-oriented security controls)"
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
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

    var app = builder.Build();

    // Global Exception Handling Middleware
    app.UseMiddleware<ExceptionMiddleware>();

    // Swagger UI
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Healthcare CRM API v1");
        c.RoutePrefix = "swagger";
    });

    app.UseCors("HealthcareCrmCorsPolicy");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Auto-seed Database on Startup
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        var context = services.GetRequiredService<ApplicationDbContext>();
        var hasher = services.GetRequiredService<IPasswordHasher>();

        try
        {
            logger.LogInformation("Initializing database and verifying schema...");
            await DatabaseSeeder.SeedAsync(context, hasher, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during database initialization or seeding.");
        }
    }

    Log.Information("Starting Healthcare CRM Web API on .NET 8...");
    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
