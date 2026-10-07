using FlowForge.Domain.Common;

namespace FlowForge.Domain.Workflows;

public sealed class WorkflowDefinition(Guid id, string name, string? description, DateTime createdAtUtc)
{
    public Guid Id { get; private set; } = DomainGuard.AgainstEmpty(id);
    public string Name { get; private set; } = DomainGuard.AgainstNullOrWhiteSpace(name);
    public string? Description { get; private set; } = description;
    public DateTime CreatedAtUtc { get; private set; } = createdAtUtc;

    public void Rename(string newName) => Name = DomainGuard.AgainstNullOrWhiteSpace(newName.Trim());
    
    public void UpdateDescription(string? description) =>  Description = description;
}