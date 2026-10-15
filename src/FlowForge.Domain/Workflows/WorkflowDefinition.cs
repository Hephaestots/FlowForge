using FlowForge.Domain.Common;

namespace FlowForge.Domain.Workflows;

public sealed class WorkflowDefinition
{
    public const int MaxDescriptionLength = 1000;

    public WorkflowDefinitionId Id { get; }
    public DisplayName Name { get; private set; }
    public string Description { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    private WorkflowDefinition(WorkflowDefinitionId id, DisplayName name, string description, DateTimeOffset createdAt)
    {
        Id = DomainGuard.AgainstNull(id, Errors.Null);
        Name = DomainGuard.AgainstNull(name, Errors.NameNull);
        Description = description;
        CreatedAt = createdAt.ToUniversalTime();
    }

    public static WorkflowDefinition Create(DisplayName name, string? description, DateTimeOffset createdAt) =>
        new(WorkflowDefinitionId.New(), name, NormalizeDescription(description, enforceLimit: true), createdAt);

    // Lenient on purpose: a stored description longer than a later-tightened cap must still load.
    public static WorkflowDefinition Rehydrate(WorkflowDefinitionId id, DisplayName name, string? description, DateTimeOffset createdAt) =>
        new(id, name, NormalizeDescription(description, enforceLimit: false), createdAt);

    public void Rename(DisplayName newName) =>
        Name = DomainGuard.AgainstNull(newName, Errors.NameNull);

    public void UpdateDescription(string? description) =>
        Description = NormalizeDescription(description, enforceLimit: true);

    private static string NormalizeDescription(string? description, bool enforceLimit)
    {
        var normalized = description?.Trim() ?? string.Empty;

        if (enforceLimit && normalized.Length > MaxDescriptionLength)
            throw new DomainException(Errors.DescriptionTooLong);

        return normalized;
    }

    public static class Errors
    {
        public static readonly DomainError Null =
            new("workflow_definition.id_null", "Workflow definition id must not be null.");

        public static readonly DomainError NameNull =
            new("workflow_definition.name_null", "Workflow definition name must not be null.");

        public static readonly DomainError DescriptionTooLong =
            new("workflow_definition.description_too_long",
                $"Workflow definition description must be at most {MaxDescriptionLength} characters.");
    }
}
