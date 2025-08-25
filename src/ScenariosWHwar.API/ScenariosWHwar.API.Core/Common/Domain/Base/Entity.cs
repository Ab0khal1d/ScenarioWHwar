namespace ScenariosWHwar.API.Core.Common.Domain.Base;

/// <summary>
/// Entities have an ID and a lifecycle (i.e. created, modified, and deleted)
/// They can be created within the domain, but not externally.
/// Enforce business rules (i.e. invariants)
/// </summary>
public abstract class Entity<TId> : Auditable, IEquatable<Entity<TId>>
    where TId : notnull
{
    public TId Id { get; protected set; }

    protected Entity(TId id)
    {
        Id = id;
    }

    protected Entity()
    {
        Id = default!;
    }

    public override bool Equals(object? obj)
    {
        return obj is Entity<TId> entity && Id.Equals(entity.Id);
    }

    public bool Equals(Entity<TId>? other)
    {
        return Equals((object?)other);
    }

    public static bool operator ==(Entity<TId> left, Entity<TId> right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Entity<TId> left, Entity<TId> right)
    {
        return !Equals(left, right);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
