using ErrorOr;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using SmartTimetableGenerator.Application.Common.Security;
using SmartTimetableGenerator.Application.UseCases.Auth.Commands.ChangePassword;
using SmartTimetableGenerator.Application.UseCases.Auth.Commands.Login;
using SmartTimetableGenerator.Application.UseCases.Auth.Queries.GetCurrentUser;
using SmartTimetableGenerator.Application.UseCases.Users.Commands.CreateUser;
using SmartTimetableGenerator.Application.UseCases.Users.Commands.ResetUserPassword;
using SmartTimetableGenerator.Application.UseCases.Users.Commands.UpdateUser;
using SmartTimetableGenerator.Application.UseCases.Users.Common;
using SmartTimetableGenerator.Application.UseCases.Users.Queries.GetUsers;
using SmartTimetableGenerator.Domain.Campuses;
using SmartTimetableGenerator.Domain.Teachers;
using SmartTimetableGenerator.Infrastructure.Identity;
using SmartTimetableGenerator.Scheduling.Tests.Common;

namespace SmartTimetableGenerator.Scheduling.Tests.Security;

/// <summary>
/// Usuarios con ASP.NET Identity real (sobre SQLite): creación por rol, reglas de sede y docente, inicio de sesión
/// con token JWT, bloqueo por intentos fallidos, cambio y restablecimiento de contraseña, y el último administrador.
/// </summary>
public sealed class UserTests : IDisposable
{
    private const string Password = "Clave2026";

    private readonly SchedulingTestHost _host = new();

    public void Dispose() => _host.Dispose();

    private async Task<TestSchool> SeedAsync()
    {
        var school = new TestSchool();
        await _host.SeedAsync(school.Seed);
        return school;
    }

    private async Task<string> CreateAsync(string email, string role, Guid? campusId = null, Guid? teacherId = null)
    {
        var result = await _host.SendAsync(new CreateUserCommand(email, $"Usuario {email}", role, Password, campusId, teacherId));
        result.IsError.Should().BeFalse(result.IsError ? result.FirstError.Description : "");
        return result.Value;
    }

    private Task<ErrorOr<AuthTokenDto>> LoginAsync(string email, string password = Password) =>
        _host.SendAsync(new LoginCommand(email, password));

    private async Task<string?> CurrentStampAsync(string userId)
    {
        using var scope = _host.CreateScope();
        var user = await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByIdAsync(userId);
        return user!.SecurityStamp;
    }

    [Fact]
    public async Task CreateUser_ForEachRole_ShouldSaveTheRoleAndOnlyTheLinksThatApply()
    {
        var school = await SeedAsync();

        var admin = await CreateAsync("  Admin@Colegio.test ", "admin", school.Campus.Id.Value, school.MathTeacher.Id.Value);
        var coordinator = await CreateAsync("coord@colegio.test", "Coordinador", school.Campus.Id.Value, school.ScienceTeacher.Id.Value);
        var teacher = await CreateAsync("docente@colegio.test", "DOCENTE", school.Campus.Id.Value, school.MathTeacher.Id.Value);

        var users = (await _host.SendAsync(new GetUsersQuery())).Value;

        users.Should().HaveCount(3);
        users.Single(u => u.Id == admin).Should().BeEquivalentTo(new UserDto(admin, "Admin@Colegio.test", "Usuario   Admin@Colegio.test", Roles.Admin, null, null, true),
            o => o.Excluding(u => u.FullName));
        users.Single(u => u.Id == coordinator).Should().BeEquivalentTo(new
        {
            Role = Roles.Coordinator,
            CampusId = (Guid?)school.Campus.Id.Value,
            TeacherId = (Guid?)school.ScienceTeacher.Id.Value
        }, "un coordinador puede dictar clase");
        users.Single(u => u.Id == teacher).Should().BeEquivalentTo(new
        {
            Role = Roles.Teacher,
            CampusId = (Guid?)null,
            TeacherId = (Guid?)school.MathTeacher.Id.Value
        });
    }

    [Fact]
    public async Task CreateUser_ThatBreaksARule_ShouldReturnTheMatchingError()
    {
        var school = await SeedAsync();
        await CreateAsync("docente@colegio.test", Roles.Teacher, teacherId: school.MathTeacher.Id.Value);

        async Task<Error> Create(string email, string role, Guid? campus = null, Guid? teacher = null, string password = Password) =>
            (await _host.SendAsync(new CreateUserCommand(email, "Nombre", role, password, campus, teacher))).FirstError;

        (await Create("a@colegio.test", "Rector")).Should().Be(UserErrors.InvalidRole);
        (await Create("a@colegio.test", Roles.Coordinator)).Should().Be(UserErrors.CampusRequired);
        (await Create("a@colegio.test", Roles.Teacher)).Should().Be(UserErrors.TeacherRequired);
        (await Create("a@colegio.test", Roles.Coordinator, campus: Guid.NewGuid())).Should().Be(CampusErrors.NotFound);
        (await Create("a@colegio.test", Roles.Teacher, teacher: Guid.NewGuid())).Should().Be(TeacherErrors.NotFound);
        (await Create("a@colegio.test", Roles.Teacher, teacher: school.MathTeacher.Id.Value)).Should().Be(UserErrors.TeacherAlreadyLinked);
        (await Create("DOCENTE@colegio.test", Roles.Admin)).Should().Be(UserErrors.DuplicateEmail);

        var weak = await Create("a@colegio.test", Roles.Admin, password: "sinnumeros");
        weak.Code.Should().Be(UserErrors.PasswordRejected("").Code);
        weak.Description.Should().Contain("número").And.Contain("mayúscula");

        var invalid = await _host.SendAsync(new CreateUserCommand("no-es-correo", "", Roles.Admin, "corta"));
        invalid.Errors.Should().HaveCount(3).And.OnlyContain(e => e.Type == ErrorType.Validation);

        (await _host.SendAsync(new GetUsersQuery())).Value.Should().ContainSingle();
    }

    [Fact]
    public async Task Login_ShouldReturnAJwtWithTheUsersClaims()
    {
        var school = await SeedAsync();
        var userId = await CreateAsync("coord@colegio.test", Roles.Coordinator, school.Campus.Id.Value);

        var result = await LoginAsync("COORD@colegio.test");

        result.IsError.Should().BeFalse();
        var login = result.Value;
        login.TokenType.Should().Be("Bearer");
        login.User.Id.Should().Be(userId);
        login.User.Role.Should().Be(Roles.Coordinator);
        login.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddHours(8), TimeSpan.FromMinutes(1));

        var token = new JsonWebTokenHandler().ReadJsonWebToken(login.AccessToken);
        token.Issuer.Should().Be("SmartTimetableGenerator");
        token.Audiences.Should().Equal("SmartTimetableGenerator");
        token.Alg.Should().Be("HS256");
        token.GetClaim(AppClaims.UserId).Value.Should().Be(userId);
        token.GetClaim(AppClaims.Role).Value.Should().Be(Roles.Coordinator);
        token.GetClaim(AppClaims.CampusId).Value.Should().Be(school.Campus.Id.Value.ToString());
        token.TryGetClaim(AppClaims.TeacherId, out _).Should().BeFalse();
        token.GetClaim(AppClaims.SecurityStamp).Value.Should().Be(await CurrentStampAsync(userId));

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(login.AccessToken, new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidIssuer = "SmartTimetableGenerator",
            ValidAudience = "SmartTimetableGenerator",
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(SchedulingTestHost.JwtKey))
        });
        validation.IsValid.Should().BeTrue("el token debe estar firmado con la llave configurada");
    }

    [Fact]
    public async Task Login_WithAWrongPasswordOrAnUnknownOrInactiveUser_ShouldFailWithTheSameError()
    {
        await SeedAsync();
        var userId = await CreateAsync("admin2@colegio.test", Roles.Admin);
        await CreateAsync("admin@colegio.test", Roles.Admin);
        (await _host.SendAsync(new UpdateUserCommand("admin2@colegio.test", "Admin dos", Roles.Admin, IsActive: false) { UserId = userId }))
            .IsError.Should().BeFalse();

        (await LoginAsync("admin@colegio.test", "Incorrecta1")).FirstError.Should().Be(UserErrors.InvalidCredentials);
        (await LoginAsync("nadie@colegio.test")).FirstError.Should().Be(UserErrors.InvalidCredentials);
        (await LoginAsync("admin2@colegio.test")).FirstError.Should().Be(UserErrors.InvalidCredentials, "un usuario inactivo no entra");
        (await LoginAsync("admin@colegio.test")).IsError.Should().BeFalse();
    }

    [Fact]
    public async Task Login_AfterFiveFailures_ShouldLockTheAccount_UntilTheAdminResetsThePassword()
    {
        await SeedAsync();
        var userId = await CreateAsync("docente@colegio.test", Roles.Admin);

        for (var i = 1; i <= 4; i++)
            (await LoginAsync("docente@colegio.test", "Incorrecta1")).FirstError.Should().Be(UserErrors.InvalidCredentials);
        (await LoginAsync("docente@colegio.test", "Incorrecta1")).FirstError.Should().Be(UserErrors.LockedOut);
        (await LoginAsync("docente@colegio.test")).FirstError.Should().Be(UserErrors.LockedOut, "bloqueada aun con la contraseña correcta");

        var weakReset = await _host.SendAsync(new ResetUserPasswordCommand("sinnumeros") { UserId = userId });
        weakReset.FirstError.Code.Should().Be(UserErrors.PasswordRejected("").Code);
        (await LoginAsync("docente@colegio.test")).FirstError.Should().Be(UserErrors.LockedOut, "un restablecimiento rechazado no cambia nada");

        (await _host.SendAsync(new ResetUserPasswordCommand("Nueva2026") { UserId = userId })).IsError.Should().BeFalse();

        (await LoginAsync("docente@colegio.test")).FirstError.Should().Be(UserErrors.InvalidCredentials, "la contraseña anterior ya no sirve");
        (await LoginAsync("docente@colegio.test", "Nueva2026")).IsError.Should().BeFalse();
    }

    [Fact]
    public async Task ChangePassword_ShouldRequireTheCurrentOne_AndChangeTheSecurityStamp()
    {
        await SeedAsync();
        var userId = await CreateAsync("coord@colegio.test", Roles.Admin);
        var stampBefore = await CurrentStampAsync(userId);
        _host.User.UserId = userId;

        var wrong = await _host.SendAsync(new ChangePasswordCommand("Incorrecta1", "Nueva2026"));
        var weak = await _host.SendAsync(new ChangePasswordCommand(Password, "sinnumeros"));
        var ok = await _host.SendAsync(new ChangePasswordCommand(Password, "Nueva2026"));

        wrong.FirstError.Should().Be(UserErrors.WrongPassword);
        weak.FirstError.Code.Should().Be(UserErrors.PasswordRejected("").Code);
        ok.IsError.Should().BeFalse();
        (await CurrentStampAsync(userId)).Should().NotBe(stampBefore, "los tokens anteriores dejan de valer");
        (await LoginAsync("coord@colegio.test", "Nueva2026")).IsError.Should().BeFalse();
    }

    [Fact]
    public async Task GetCurrentUser_ShouldReturnTheSignedInUser()
    {
        var school = await SeedAsync();
        var userId = await CreateAsync("docente@colegio.test", Roles.Teacher, teacherId: school.ScienceTeacher.Id.Value);
        _host.SignInAs(Roles.Teacher, teacherId: school.ScienceTeacher.Id);
        _host.User.UserId = userId;

        var me = await _host.SendAsync(new GetCurrentUserQuery());

        me.Value.Email.Should().Be("docente@colegio.test");
        me.Value.Role.Should().Be(Roles.Teacher);
        me.Value.TeacherId.Should().Be(school.ScienceTeacher.Id.Value);

        _host.SignInAs(null);
        (await _host.SendAsync(new GetCurrentUserQuery())).FirstError.Should().Be(SecurityErrors.Unauthenticated);
    }

    [Fact]
    public async Task UpdateUser_ShouldChangeRoleAndLinks_AndInvalidateTheirTokens()
    {
        var school = await SeedAsync();
        await CreateAsync("admin@colegio.test", Roles.Admin);
        var userId = await CreateAsync("persona@colegio.test", Roles.Teacher, teacherId: school.MathTeacher.Id.Value);
        var stampBefore = await CurrentStampAsync(userId);

        var result = await _host.SendAsync(new UpdateUserCommand("persona@colegio.test", "Persona", Roles.Coordinator, school.OtherCampus.Id.Value)
        {
            UserId = userId
        });

        result.IsError.Should().BeFalse();
        var user = (await _host.SendAsync(new GetUsersQuery())).Value.Single(u => u.Id == userId);
        user.Should().BeEquivalentTo(new UserDto(userId, "persona@colegio.test", "Persona", Roles.Coordinator, school.OtherCampus.Id.Value, null, true));
        (await CurrentStampAsync(userId)).Should().NotBe(stampBefore);

        var sameAgain = await _host.SendAsync(new UpdateUserCommand("persona@colegio.test", "Persona Pérez", Roles.Coordinator, school.OtherCampus.Id.Value)
        {
            UserId = userId
        });
        sameAgain.IsError.Should().BeFalse();
        var stampAfterRename = await CurrentStampAsync(userId);
        var renamed = await _host.SendAsync(new UpdateUserCommand("persona@colegio.test", "Persona Pérez G.", Roles.Coordinator, school.OtherCampus.Id.Value)
        {
            UserId = userId
        });
        renamed.IsError.Should().BeFalse();
        (await CurrentStampAsync(userId)).Should().Be(stampAfterRename, "cambiar el nombre no cierra la sesión");
    }

    [Fact]
    public async Task UpdateUser_ShouldRejectDuplicatesUnknownUsersAndLinkingATeacherTwice()
    {
        var school = await SeedAsync();
        await CreateAsync("uno@colegio.test", Roles.Teacher, teacherId: school.MathTeacher.Id.Value);
        var two = await CreateAsync("dos@colegio.test", Roles.Teacher, teacherId: school.ScienceTeacher.Id.Value);

        (await _host.SendAsync(new UpdateUserCommand("UNO@colegio.test", "Dos", Roles.Teacher, TeacherId: school.ScienceTeacher.Id.Value) { UserId = two }))
            .FirstError.Should().Be(UserErrors.DuplicateEmail);
        (await _host.SendAsync(new UpdateUserCommand("dos@colegio.test", "Dos", Roles.Teacher, TeacherId: school.MathTeacher.Id.Value) { UserId = two }))
            .FirstError.Should().Be(UserErrors.TeacherAlreadyLinked);
        (await _host.SendAsync(new UpdateUserCommand("x@colegio.test", "X", Roles.Admin) { UserId = "no-existe" }))
            .FirstError.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task TheLastActiveAdmin_CannotBeDeactivatedNorDemoted()
    {
        await SeedAsync();
        var admin = await CreateAsync("admin@colegio.test", Roles.Admin);

        var deactivate = await _host.SendAsync(new UpdateUserCommand("admin@colegio.test", "Admin", Roles.Admin, IsActive: false) { UserId = admin });
        var demote = await _host.SendAsync(new UpdateUserCommand("admin@colegio.test", "Admin", Roles.Teacher, TeacherId: null) { UserId = admin });

        deactivate.FirstError.Should().Be(UserErrors.LastAdmin);
        demote.FirstError.Should().Be(UserErrors.TeacherRequired, "primero se validan los datos del rol nuevo");

        var second = await CreateAsync("admin2@colegio.test", Roles.Admin);
        (await _host.SendAsync(new UpdateUserCommand("admin@colegio.test", "Admin", Roles.Admin, IsActive: false) { UserId = admin }))
            .IsError.Should().BeFalse("ya hay otro administrador activo");
        (await _host.SendAsync(new UpdateUserCommand("admin2@colegio.test", "Admin", Roles.Admin, IsActive: false) { UserId = second }))
            .FirstError.Should().Be(UserErrors.LastAdmin);
    }

    [Fact]
    public async Task UserManagement_ShouldBeOnlyForAdmins()
    {
        var school = await SeedAsync();
        _host.SignInAs(Roles.Coordinator, school.Campus.Id);

        (await _host.SendAsync(new GetUsersQuery())).FirstError.Should().Be(SecurityErrors.Forbidden);
        (await _host.SendAsync(new CreateUserCommand("x@colegio.test", "X", Roles.Admin, Password))).FirstError.Should().Be(SecurityErrors.Forbidden);
        (await _host.SendAsync(new UpdateUserCommand("x@colegio.test", "X", Roles.Admin) { UserId = "x" })).FirstError.Should().Be(SecurityErrors.Forbidden);
        (await _host.SendAsync(new ResetUserPasswordCommand("Nueva2026") { UserId = "x" })).FirstError.Should().Be(SecurityErrors.Forbidden);

        _host.SignInAs(null);
        (await LoginAsync("nadie@colegio.test")).FirstError.Should().Be(UserErrors.InvalidCredentials, "el inicio de sesión no pide sesión");
    }
}
