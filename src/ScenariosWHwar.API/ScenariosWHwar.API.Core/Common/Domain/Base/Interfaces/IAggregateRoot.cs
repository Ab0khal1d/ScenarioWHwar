namespace ScenariosWHwar.API.Core.Common.Domain.Base.Interfaces;

public interface IAggregateRoot
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
    void AddDomainEvent(IDomainEvent domainEvent);
    IReadOnlyList<IDomainEvent> PopDomainEvents();
}