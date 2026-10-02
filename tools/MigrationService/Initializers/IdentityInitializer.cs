using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartTimetableGenerator.Application.Common.Security;
using SmartTimetableGenerator.Infrastructure.Identity;
using SmartTimetableGenerator.Infrastructure.Persistence;

namespace MigrationService.Initializers;

/// <summary>
/// Crea los roles y el primer administrador (sección <c>InitialAdmin</c> de la configuración).
/// En Development agrega un coordinador de la sede principal y un docente de ejemplo.
/// </summary>
public class IdentityInitializer(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ApplicationDbContext dbContext,
    IConfiguration configuration,
    ILogger<IdentityInitializer> logger)
{
    public const string SampleCoordinatorEmail = "coordinador@colegio.local";
    public const string SampleCoordinatorPassword = "Coordinador2026";
    public const string SampleTeacherEmail = "docente@colegio.local";
    public const string SampleTeacherPassword = "Docente2026";

    public async Task SeedAsync(bool isDevelopment, CancellationToken cancellationToken)
    {
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await EnsureSucceededAsync(roleManager.CreateAsync(new IdentityRole(role)), $"rol {role}");
        }

        var email = configuration["InitialAdmin:Email"];
        var password = configuration["InitialAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            if (!await userManager.Users.AnyAsync(cancellationToken))
                logger.LogWarning("No hay usuarios y falta la sección InitialAdmin (Email y Password): no se creó el administrador");
        }
        else
        {
            await CreateIfMissingAsync(email, "Administrador", Roles.Admin, password, null, null);
        }

        if (!isDevelopment)
            return;

        var campus = await dbContext.Campuses.AsNoTracking().OrderBy(c => c.Name).FirstOrDefaultAsync(cancellationToken);
        var teacher = await dbContext.Teachers.AsNoTracking()
            .OrderBy(t => t.LastName).ThenBy(t => t.FirstName)
            .FirstOrDefaultAsync(cancellationToken);

        if (campus is not null)
            await CreateIfMissingAsync(SampleCoordinatorEmail, "Coordinador de ejemplo", Roles.Coordinator, SampleCoordinatorPassword, campus.Id.Value, null);

        if (teacher is not null)
            await CreateIfMissingAsync(SampleTeacherEmail, teacher.FullName, Roles.Teacher, SampleTeacherPassword, null, teacher.Id.Value);
    }

    private async Task CreateIfMissingAsync(string email, string fullName, string role, string password, Guid? campusId, Guid? teacherId)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            CampusId = campusId,
            TeacherId = teacherId,
            IsActive = true
        };

        await EnsureSucceededAsync(userManager.CreateAsync(user, password), $"usuario {email}");
        await EnsureSucceededAsync(userManager.AddToRoleAsync(user, role), $"rol de {email}");
        logger.LogInformation("Usuario creado con el rol {Role}", role);
    }

    private static async Task EnsureSucceededAsync(Task<IdentityResult> operation, string what)
    {
        var result = await operation;
        if (!result.Succeeded)
            throw new InvalidOperationException($"No se pudo crear {what}: {string.Join(" ", result.Errors.Select(e => e.Description))}");
    }
}
