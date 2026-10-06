using FlowForge.Domain.Common;

namespace FlowForge.Domain.Workflows;

public sealed class Workflow
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }    
    public string? Description { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    
    public Workflow(Guid id, string name, string? description, DateTime createdAtUtc)
    {
        DomainGuard.NotNullOrWhiteSpace(name, nameof(name));
        
        Id = id;
        Name = name;
        Description = description;
        CreatedAtUtc = createdAtUtc;
    }

    public void Rename(string newName)
    {
        DomainGuard.NotNullOrWhiteSpace(newName, nameof(newName));
        
        Name = newName;
    }
    
    public void UpdateDescription(string? description) =>  Description = description;
}