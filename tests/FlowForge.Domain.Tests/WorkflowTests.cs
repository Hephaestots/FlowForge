using FlowForge.Domain.Workflows;

namespace FlowForge.Domain.Tests;

public sealed class WorkflowTests
{
    [Fact]
    public void Constructor_WithValidValues_CreateWorkflow()
    {
        var id = Guid.NewGuid();
        var createdAtUtc = DateTime.UtcNow;
        
        Workflow workflow = new(
            id, 
            "My workflow", 
            "My workflow description", 
            createdAtUtc);
        
        Assert.Equal(id, workflow.Id);
        Assert.Equal("My workflow", workflow.Name);
        Assert.Equal("My workflow description", workflow.Description);
        Assert.Equal(createdAtUtc, workflow.CreatedAtUtc);
    }

    [Fact]
    public void Constructor_WithEmptyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Workflow(
            Guid.NewGuid(),
            " ",
            null,
            DateTime.UtcNow));
    }
}