using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HealthcareCRM.Application.Common;
using HealthcareCRM.Application.Interfaces;
using HealthcareCRM.Infrastructure.Persistence;
using HealthcareCRM.Infrastructure.Security;
using HealthcareCRM.Infrastructure.Services;

namespace HealthcareCRM.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=.\\SQLEXPRESS;Database=HealthcareCRM_Db;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;";

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Security & Utilities
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IDateTimeProvider, DefaultDateTimeProvider>();

        // Dev Email & 30-Second OTP
        services.AddSingleton<IDevEmailService, DevEmailService>();
        services.AddScoped<IOtpService, OtpService>();

        // Domain Services
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IClinicalService, ClinicalService>();
        services.AddScoped<ILabService, LabService>();
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}
