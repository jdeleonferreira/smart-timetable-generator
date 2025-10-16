namespace STG.Domain.Entities.Base;

/// <summary>
/// Base type for aggregate roots in the domain model.
/// In DDD terms, an aggregate root is the single entry point
/// through which the aggregate's state can be modified or queried.
/// </summary>
public abstract class AggregateRoot : Entity
{
    /// <summary>
    /// Initializes the aggregate root and marks creation metadata.
    /// </summary>
    protected AggregateRoot(string? createdBy = null)
    {
        SetCreated(createdBy);
    }

    /// <summary>
    /// Marks the aggregate as modified, updating audit fields.
    /// Call this from domain methods that change the aggregate state.
    /// </summary>
    protected void MarkModified(string? modifiedBy = null)
    {
        SetModified(modifiedBy);
    }
}
