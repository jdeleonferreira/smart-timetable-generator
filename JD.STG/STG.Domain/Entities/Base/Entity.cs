namespace STG.Domain.Entities.Base;

/// <summary>
/// Base type for all domain entities, providing identity, auditing,
/// and identity-based equality semantics. Keeps the domain decoupled from EF Core.
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    // ---------------------------
    // Identity
    // ---------------------------

    /// <summary>
    /// Stable unique identifier of the entity. May be <see cref="Guid.Empty"/> while transient.
    /// </summary>
    public Guid Id { get; protected set; }

    // ---------------------------
    // Auditing
    // ---------------------------

    /// <summary>
    /// UTC timestamp of the entity creation.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Optional user or system identifier that created this entity.
    /// </summary>
    public string? CreatedBy { get; private set; }

    /// <summary>
    /// UTC timestamp of the last modification, if any.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; private set; }

    /// <summary>
    /// Optional user or system identifier that last modified this entity.
    /// </summary>
    public string? ModifiedBy { get; private set; }

    /// <summary>
    /// Protected constructor to support EF Core materialization and derived types.
    /// </summary>
    protected Entity()
    {
        // Allow EF to materialize with Guid.Empty; factories/aggregates may set Id explicitly.
        Id = Guid.Empty;
    }

    /// <summary>
    /// Indicates whether the entity is transient (i.e., not yet assigned a persistent identity).
    /// </summary>
    public bool IsTransient => Id == Guid.Empty;

    // ---------------------------
    // Auditing helpers
    // ---------------------------

    /// <summary>
    /// Sets creation audit fields and identity (if missing).
    /// Call this from factories or aggregate constructors when the entity is born.
    /// </summary>
    /// <param name="createdBy">User or system that creates the entity (optional).</param>
    /// <param name="at">Creation timestamp; defaults to <see cref="DateTimeOffset.UtcNow"/>.</param>
    /// <param name="id">Explicit identity to assign; if null and transient, a new Guid is generated.</param>
    protected void SetCreated(string? createdBy = null, DateTimeOffset? at = null, Guid? id = null)
    {
        if (!IsTransient && CreatedAt != default) return;

        Id = id ?? (IsTransient ? Guid.NewGuid() : Id);
        CreatedAt = at ?? DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
        ModifiedAt = null;
        ModifiedBy = null;
    }

    /// <summary>
    /// Sets modification audit fields. Call this from domain methods whenever state changes.
    /// </summary>
    /// <param name="modifiedBy">User or system that performs the modification (optional).</param>
    /// <param name="at">Modification timestamp; defaults to <see cref="DateTimeOffset.UtcNow"/>.</param>
    protected void SetModified(string? modifiedBy = null, DateTimeOffset? at = null)
    {
        ModifiedAt = at ?? DateTimeOffset.UtcNow;
        ModifiedBy = modifiedBy;
    }

    // ---------------------------
    // Equality by identity
    // ---------------------------

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Entity);

    /// <summary>
    /// Entities are equal if they share the same non-empty <see cref="Id"/> and exact runtime type.
    /// Transient entities (<see cref="IsTransient"/>) are never considered equal by identity.
    /// </summary>
    public bool Equals(Entity? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;

        if (IsTransient || other.IsTransient) return false;

        return Id == other.Id && GetType() == other.GetType();
    }

    /// <summary>
    /// Returns a stable hash code based on runtime type and <see cref="Id"/> when persistent;
    /// falls back to the base implementation for transient instances.
    /// </summary>
    public override int GetHashCode()
    {
        if (IsTransient) return base.GetHashCode();
        return HashCode.Combine(GetType(), Id);
    }

    public static bool operator ==(Entity? a, Entity? b) => a is null ? b is null : a.Equals(b);
    public static bool operator !=(Entity? a, Entity? b) => !(a == b);
}
