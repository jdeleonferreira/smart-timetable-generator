using System.Globalization;
using System.Text;
using ErrorOr;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.Common.Security;
using SmartTimetableGenerator.Application.UseCases.Users.Common;

namespace SmartTimetableGenerator.Infrastructure.Identity;

internal sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<JwtOptions> jwtOptions,
    TimeProvider timeProvider) : IIdentityService
{
    public async Task<ErrorOr<AuthTokenDto>> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
            return UserErrors.InvalidCredentials;

        if (await userManager.IsLockedOutAsync(user))
            return UserErrors.LockedOut;

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.AccessFailedAsync(user);
            return await userManager.IsLockedOutAsync(user) ? UserErrors.LockedOut : UserErrors.InvalidCredentials;
        }

        await userManager.ResetAccessFailedCountAsync(user);

        var dto = await ToDtoAsync(user);
        var expiresAt = timeProvider.GetUtcNow().AddMinutes(jwtOptions.Value.ExpirationMinutes);
        return new AuthTokenDto(CreateToken(user, dto, expiresAt), "Bearer", expiresAt, dto);
    }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken)
    {
        var users = await userManager.Users.AsNoTracking().ToListAsync(cancellationToken);

        var result = new List<UserDto>(users.Count);
        foreach (var user in users)
            result.Add(await ToDtoAsync(user));

        return result.OrderBy(u => u.FullName, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    public async Task<ErrorOr<UserDto>> GetUserAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return UserErrors.NotFound;

        return await ToDtoAsync(user);
    }

    public async Task<ErrorOr<string>> CreateUserAsync(UserData data, string password, CancellationToken cancellationToken)
    {
        if (await userManager.FindByEmailAsync(data.Email) is not null)
            return UserErrors.DuplicateEmail;

        if (data.TeacherId is { } teacherId && await userManager.Users.AnyAsync(u => u.TeacherId == teacherId, cancellationToken))
            return UserErrors.TeacherAlreadyLinked;

        var user = new ApplicationUser
        {
            UserName = data.Email,
            Email = data.Email,
            EmailConfirmed = true,
            FullName = data.FullName,
            CampusId = data.CampusId,
            TeacherId = data.TeacherId,
            IsActive = true
        };

        var created = await userManager.CreateAsync(user, password);
        if (!created.Succeeded)
            return ToErrors(created);

        await EnsureRoleAsync(data.Role);
        var inRole = await userManager.AddToRoleAsync(user, data.Role);
        if (!inRole.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return ToErrors(inRole);
        }

        return user.Id;
    }

    public async Task<ErrorOr<Success>> UpdateUserAsync(string userId, UserData data, bool isActive, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return UserErrors.NotFound;

        if (await userManager.FindByEmailAsync(data.Email) is { } other && other.Id != user.Id)
            return UserErrors.DuplicateEmail;

        if (data.TeacherId is { } teacherId &&
            await userManager.Users.AnyAsync(u => u.TeacherId == teacherId && u.Id != user.Id, cancellationToken))
        {
            return UserErrors.TeacherAlreadyLinked;
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var wasActiveAdmin = user.IsActive && currentRoles.Contains(Roles.Admin);
        var staysActiveAdmin = isActive && data.Role == Roles.Admin;
        if (wasActiveAdmin && !staysActiveAdmin)
        {
            var admins = await userManager.GetUsersInRoleAsync(Roles.Admin);
            if (!admins.Any(a => a.Id != user.Id && a.IsActive))
                return UserErrors.LastAdmin;
        }

        var accessChanged = user.IsActive != isActive || user.CampusId != data.CampusId || user.TeacherId != data.TeacherId ||
                            !currentRoles.SequenceEqual([data.Role]);

        user.Email = data.Email;
        user.UserName = data.Email;
        user.FullName = data.FullName;
        user.CampusId = data.CampusId;
        user.TeacherId = data.TeacherId;
        user.IsActive = isActive;

        var updated = await userManager.UpdateAsync(user);
        if (!updated.Succeeded)
            return ToErrors(updated);

        if (!currentRoles.SequenceEqual([data.Role]))
        {
            await EnsureRoleAsync(data.Role);
            var removed = await userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removed.Succeeded)
                return ToErrors(removed);

            var added = await userManager.AddToRoleAsync(user, data.Role);
            if (!added.Succeeded)
                return ToErrors(added);
        }

        // Los permisos cambiaron: los tokens emitidos antes dejan de valer
        if (accessChanged)
            await userManager.UpdateSecurityStampAsync(user);

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> ResetPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return UserErrors.NotFound;

        // Se valida antes de quitar la contraseña actual, para no dejar la cuenta sin contraseña
        foreach (var validator in userManager.PasswordValidators)
        {
            var valid = await validator.ValidateAsync(userManager, user, newPassword);
            if (!valid.Succeeded)
                return ToErrors(valid);
        }

        var removed = await userManager.RemovePasswordAsync(user);
        if (!removed.Succeeded)
            return ToErrors(removed);

        var added = await userManager.AddPasswordAsync(user, newPassword);
        if (!added.Succeeded)
            return ToErrors(added);

        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.ResetAccessFailedCountAsync(user);
        return Result.Success;
    }

    public async Task<ErrorOr<Success>> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return UserErrors.NotFound;

        var changed = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!changed.Succeeded)
            return ToErrors(changed);

        return Result.Success;
    }

    private async Task EnsureRoleAsync(string role)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    private async Task<UserDto> ToDtoAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new UserDto(user.Id, user.Email ?? string.Empty, user.FullName, roles.FirstOrDefault() ?? string.Empty,
            user.CampusId, user.TeacherId, user.IsActive);
    }

    private string CreateToken(ApplicationUser user, UserDto dto, DateTimeOffset expiresAt)
    {
        var options = jwtOptions.Value;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var claims = new Dictionary<string, object>
        {
            [AppClaims.UserId] = dto.Id,
            [AppClaims.Email] = dto.Email,
            [AppClaims.Name] = dto.FullName,
            [AppClaims.Role] = dto.Role,
            [AppClaims.SecurityStamp] = user.SecurityStamp ?? string.Empty
        };
        if (dto.CampusId is { } campusId)
            claims[AppClaims.CampusId] = campusId.ToString("D", CultureInfo.InvariantCulture);
        if (dto.TeacherId is { } teacherId)
            claims[AppClaims.TeacherId] = teacherId.ToString("D", CultureInfo.InvariantCulture);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Claims = claims,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(SigningKey(options), SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    internal static SymmetricSecurityKey SigningKey(JwtOptions options) => new(Encoding.UTF8.GetBytes(options.SigningKey));

    private static List<Error> ToErrors(IdentityResult result)
    {
        if (result.Errors.Any(e => e.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName)))
            return [UserErrors.DuplicateEmail];

        if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.PasswordMismatch)))
            return [UserErrors.WrongPassword];

        return [UserErrors.PasswordRejected(string.Join(" ", result.Errors.Select(e => e.Description)))];
    }
}
