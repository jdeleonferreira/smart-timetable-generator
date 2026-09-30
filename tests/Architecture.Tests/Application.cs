using MediatR;
using SmartTimetableGenerator.Architecture.UnitTests.Common;

namespace SmartTimetableGenerator.Architecture.UnitTests;

public class Application : TestBase
{
    private static readonly Type IRequestHandler = typeof(IRequestHandler<,>);

    [Fact]
    public void CommandHandlers_ShouldHaveCorrectSuffix()
    {
        var types = Types
            .InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespaceContaining("Commands")
            .And()
            .ImplementInterface(IRequestHandler);

        var result = types
            .Should()
            .HaveNameEndingWith("CommandHandler")
            .GetResult();

        result.Should().BeSuccessful();
    }

    [Fact]
    public void QueryHandlers_ShouldHaveCorrectSuffix()
    {
        var types = Types
                .InAssembly(ApplicationAssembly)
                .That()
                .ResideInNamespaceContaining("Queries")
                .And()
                .ImplementInterface(IRequestHandler);

        var result = types
            .Should()
            .HaveNameEndingWith("QueryHandler")
            .GetResult();

        result.Should().BeSuccessful();
    }

    [Fact]
    public void RequestsWithAuthorize_ShouldReturnErrorOr()
    {
        // AuthorizationBehaviour solo puede responder "no autorizado" en requests que devuelven ErrorOr
        var failing = ApplicationAssembly.GetTypes()
            .Where(t => t.GetCustomAttributes(typeof(SmartTimetableGenerator.Application.Common.Security.AuthorizeAttribute), true).Length > 0)
            .Where(t => !t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IRequest<>) &&
                typeof(ErrorOr.IErrorOr).IsAssignableFrom(i.GetGenericArguments()[0])))
            .Select(t => t.FullName)
            .ToList();

        failing.Should().BeEmpty();
    }

    [Fact]
    public void CommandsThatChangeData_ShouldRequireAuthorization()
    {
        // Todo comando exige usuario, salvo el inicio de sesión
        var failing = ApplicationAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Command", StringComparison.Ordinal) && t.IsClass && !t.IsAbstract)
            .Where(t => t.Name != "LoginCommand")
            .Where(t => t.GetCustomAttributes(typeof(SmartTimetableGenerator.Application.Common.Security.AuthorizeAttribute), true).Length == 0)
            .Select(t => t.FullName)
            .ToList();

        failing.Should().BeEmpty();
    }
}
