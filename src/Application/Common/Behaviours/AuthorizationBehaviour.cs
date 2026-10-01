using System.Reflection;
using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Application.Common.Security;

namespace SmartTimetableGenerator.Application.Common.Behaviours;

/// <summary>
/// Aplica <see cref="AuthorizeAttribute"/>: sin usuario devuelve <see cref="SecurityErrors.Unauthenticated"/>;
/// sin ninguno de los roles pedidos, <see cref="SecurityErrors.Forbidden"/>. Corre antes de la validación.
/// </summary>
public class AuthorizationBehaviour<TRequest, TResponse>(ICurrentUserService currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IErrorOr
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var attributes = typeof(TRequest).GetCustomAttributes<AuthorizeAttribute>().ToList();
        if (attributes.Count == 0)
            return await next(cancellationToken);

        if (!currentUser.IsAuthenticated)
            return (dynamic)new List<Error> { SecurityErrors.Unauthenticated };

        // Cada atributo con roles debe cumplirse (alguno de sus roles)
        foreach (var attribute in attributes.Where(a => !string.IsNullOrWhiteSpace(a.Roles)))
        {
            var roles = attribute.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!roles.Any(currentUser.IsInRole))
                return (dynamic)new List<Error> { SecurityErrors.Forbidden };
        }

        return await next(cancellationToken);
    }
}
