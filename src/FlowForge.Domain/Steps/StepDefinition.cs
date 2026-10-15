using FlowForge.Domain.Common;
using FlowForge.Domain.Handlers;

namespace FlowForge.Domain.Steps;

public sealed record StepDefinition
{
    public StepId Id { get; }
    public DisplayName Name { get; }
    public HandlerKey HandlerKey { get; }

    private StepDefinition(StepId id, DisplayName name, HandlerKey handlerKey)
    {
        Id = DomainGuard.AgainstNull(id, Errors.IdNull);
        Name = DomainGuard.AgainstNull(name, Errors.NameNull);
        HandlerKey = DomainGuard.AgainstNull(handlerKey, Errors.HandlerKeyNull);
    }

    public static StepDefinition Create(StepId id, DisplayName name, HandlerKey handlerKey) =>
        new(id, name, handlerKey);

    public static StepDefinition Rehydrate(StepId id, DisplayName name, HandlerKey handlerKey) =>
        new(id, name, handlerKey);

    public StepDefinition WithDisplayName(DisplayName displayName) =>
        new(Id, displayName, HandlerKey);

    public StepDefinition WithHandlerKey(HandlerKey handlerKey) =>
        new(Id, Name, handlerKey);

    public bool Equals(StepDefinition? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        if (other is null)
            return false;

        return
            Id == other.Id &&
            HandlerKey == other.HandlerKey;
    }

    public override int GetHashCode() => HashCode.Combine(Id, HandlerKey);

    public static class Errors
    {
        public static readonly DomainError IdNull =
            new("step_definition.id_null", "Step definition id must not be null.");

        public static readonly DomainError NameNull =
            new("step_definition.name_null", "Step definition name must not be null.");

        public static readonly DomainError HandlerKeyNull =
            new("step_definition.handler_key_null", "Step definition handler key must not be null.");
    }
}