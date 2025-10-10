namespace STG.Application.Abstractions.Persistence;

/// <summary>
/// Coordinates atomic persistence operations.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all changes made in the current context.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
