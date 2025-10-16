using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace STG.Api.Errors;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next; _logger = logger;
    }

    public async Task Invoke(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");

            var (status, title) = Map(ex);
            var problem = new ProblemDetails
            {
                Title = title,
                Status = (int)status,
                Detail = ex.Message,
                Instance = ctx.TraceIdentifier
            };

            ctx.Response.ContentType = "application/problem+json";
            ctx.Response.StatusCode = problem.Status ?? (int)HttpStatusCode.InternalServerError;
            await ctx.Response.WriteAsJsonAsync(problem);
        }
    }

    private static (HttpStatusCode status, string title) Map(Exception ex) =>
        ex switch
        {
            ArgumentOutOfRangeException => (HttpStatusCode.BadRequest, "Invalid range"),
            ArgumentException => (HttpStatusCode.BadRequest, "Invalid argument"),
            InvalidOperationException => (HttpStatusCode.Conflict, "Operation conflict"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Not found"),
            DbUpdateException => (HttpStatusCode.Conflict, "Persistence error"),
            _ => (HttpStatusCode.InternalServerError, "Unexpected error")
        };
}
