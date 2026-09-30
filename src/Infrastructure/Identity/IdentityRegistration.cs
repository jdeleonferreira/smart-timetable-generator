using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Infrastructure.Persistence;

namespace SmartTimetableGenerator.Infrastructure.Identity;

public static class IdentityRegistration
{
    /// <summary>
    /// Cuentas de usuario sobre la base de datos de la aplicación: reglas de contraseña, bloqueo tras intentos fallidos y roles.
    /// </summary>
    public static IdentityBuilder AddIdentityStores(this IServiceCollection services) =>
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddErrorDescriber<SpanishIdentityErrorDescriber>();

    /// <summary>
    /// Servicio de usuarios y tokens (sin autenticación HTTP; útil para pruebas y herramientas).
    /// </summary>
    public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityStores();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => o.HasValidKey, $"Configure {JwtOptions.SectionName}:SigningKey con al menos {JwtOptions.MinimumKeyBytes} caracteres")
            .ValidateOnStart();

        services.AddScoped<IIdentityService, IdentityService>();
        return services;
    }

    /// <summary>
    /// Autenticación con tokens JWT (Authorization: Bearer). Además de la firma y la vigencia, en cada petición se
    /// comprueba que el usuario siga activo y que su sello de seguridad no haya cambiado (contraseña, rol o sede).
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityServices(configuration);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, jwt) =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Value.Issuer,
                    ValidAudience = jwt.Value.Audience,
                    IssuerSigningKey = IdentityService.SigningKey(jwt.Value),
                    ValidateIssuerSigningKey = true,
                    NameClaimType = AppClaims.Name,
                    RoleClaimType = AppClaims.Role,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                        var userId = context.Principal?.FindFirstValue(AppClaims.UserId);
                        var user = userId is null ? null : await userManager.FindByIdAsync(userId);

                        if (user is null || !user.IsActive ||
                            user.SecurityStamp != context.Principal?.FindFirstValue(AppClaims.SecurityStamp))
                        {
                            context.Fail("La sesión ya no es válida; inicie sesión de nuevo");
                        }
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }
}
