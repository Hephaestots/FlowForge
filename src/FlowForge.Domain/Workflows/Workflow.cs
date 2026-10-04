namespace FlowForge.Domain.Workflows;

public sealed class Workflow
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }    
    public string? Description { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    
    public Workflow(Guid id, string name, string? description, DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));
        
        Id = id;
        Name = name;
        Description = description;
        CreatedAtUtc = createdAtUtc;
    }
}