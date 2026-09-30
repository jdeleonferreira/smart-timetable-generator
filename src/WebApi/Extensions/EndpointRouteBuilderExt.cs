namespace SmartTimetableGenerator.WebApi.Extensions;

public static class EndpointRouteBuilderExt
{
    /// <summary>
    /// Used for GET endpoints that return one or more items.
    /// </summary>
    public static RouteHandlerBuilder ProducesGet<T>(this RouteHandlerBuilder builder) => builder
        .Produces<T>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

    /// <summary>
    /// Used for POST endpoints that creates a single item.
    /// </summary>
    public static RouteHandlerBuilder ProducesPost<T>(this RouteHandlerBuilder builder) => builder
        .Produces<T>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status500InternalServerError);

    /// <summary>
    /// Used for PUT endpoints that updates a single item.
    /// </summary>
    public static RouteHandlerBuilder ProducesPut(this RouteHandlerBuilder builder) => builder
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status500InternalServerError);

    /// <summary>
    /// Used for DELETE endpoints that deletes a single item.
    /// </summary>
    public static RouteHandlerBuilder ProducesDelete(this RouteHandlerBuilder builder) => builder
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

    /// <summary>
    /// Used for GET endpoints that return a document (PDF or Word).
    /// </summary>
    public static RouteHandlerBuilder ProducesDocument(this RouteHandlerBuilder builder) => builder
        .Produces<byte[]>(StatusCodes.Status200OK, "application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
}
