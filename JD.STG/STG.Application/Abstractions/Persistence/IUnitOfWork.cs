namespace STG.Application.Abstractions.Persistence;

/// <summary>
/// Coordinates the work of multiple repositories against a single DbContext/transaction.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
