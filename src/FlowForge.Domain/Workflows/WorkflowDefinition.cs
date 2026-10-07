using FlowForge.Domain.Common;

namespace FlowForge.Domain.Workflows;

public sealed class WorkflowDefinition(Guid id, string name, string? description, DateTime createdAtUtc)
{
    public Guid Id { get; } = DomainGuard.AgainstEmpty(id);
    public DisplayName Name { get; private set; } = new(name);
    public string? Description { get; private set; } = description;
    public DateTime CreatedAtUtc { get; } = createdAtUtc;

    public void Rename(string newName) => Name = new DisplayName(newName);
    
    public void UpdateDescription(string? description) =>  Description = description;
}