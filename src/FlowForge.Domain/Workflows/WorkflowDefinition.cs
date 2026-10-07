using FlowForge.Domain.Common;

namespace FlowForge.Domain.Workflows;

public sealed class WorkflowDefinition
{
    public WorkflowDefinitionId Id { get; }
    public DisplayName Name { get; private set; }
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    private WorkflowDefinition(string name, string? description, DateTimeOffset createdAt) :
        this(WorkflowDefinitionId.New(), name, description, createdAt)
    { }

    private WorkflowDefinition(WorkflowDefinitionId id, string name, string? description, DateTimeOffset createdAt)
    {
        Id = DomainGuard.AgainstNull(id);
        Name = new DisplayName(name);
        Description = description;
        CreatedAt = createdAt.ToUniversalTime();
    }

    public static WorkflowDefinition Create(string name, string? description, DateTimeOffset createdAt) =>
        new(name, description, createdAt);
    
    public static WorkflowDefinition Rehydrate(WorkflowDefinitionId id, string name, string? description, DateTimeOffset createdAt) => 
        new(id, name, description, createdAt);

    public void Rename(string newName) => Name = new DisplayName(newName);
    
    public void UpdateDescription(string? description) => Description = description;
}