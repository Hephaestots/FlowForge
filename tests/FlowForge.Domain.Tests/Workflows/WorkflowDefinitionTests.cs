using FlowForge.Domain.Common;
using FlowForge.Domain.Workflows;

namespace FlowForge.Domain.Tests.Workflows;

public sealed class WorkflowDefinitionTests
{
    [Fact]
    public void Constructor_WithValidValues_ShouldCreateWorkflowDefinition()
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdAtUtc = DateTime.UtcNow;
        
        // Act
        WorkflowDefinition workflow = new(
            id, 
            "  My workflow  ", 
            "My workflow description", 
            createdAtUtc);
        
        // Assert
        Assert.Equal(id, workflow.Id);
        Assert.Equal(new DisplayName("My workflow"), workflow.Name);
        Assert.Equal("My workflow description", workflow.Description);
        Assert.Equal(createdAtUtc, workflow.CreatedAtUtc);
    }

    [Fact]
    public void Constructor_WhenIdIsEmpty_ShouldThrowDomainException()
    {
        // Assert
        Assert.Throws<DomainException>(() => new WorkflowDefinition(
            Guid.Empty,
            "My workflow",
            null,
            DateTime.UtcNow));
    }

    [Fact]
    public void Rename_WithValidName_ShouldRenameWorkflowDefinition()
    {
        // Arrange
        var workflow = new WorkflowDefinition(
            Guid.NewGuid(),
            "Old name",
            null,
            DateTime.UtcNow);
        
        // Act
        workflow.Rename("  New name  ");
        
        // Assert
        Assert.Equal(new DisplayName("New name"), workflow.Name);
    }

    [Fact]
    public void UpdateDescription_WhenDescriptionIsValidString_ShouldUpdateWorkflowDefinitionDescription()
    {
        // Arrange
        var workflow = new WorkflowDefinition(
            Guid.NewGuid(),
            "workflow 1",
            null,
            DateTime.UtcNow);
        
        // Act
        workflow.UpdateDescription("My workflow description.");
        
        // Assert
        Assert.Equal("My workflow description.", workflow.Description);
    }
    
    [Fact]
    public void UpdateDescription_WhenDescriptionIsNull_ShouldClearWorkflowDefinitionDescription()
    {
        // Arrange
        var workflow = new WorkflowDefinition(
            Guid.NewGuid(),
            "workflow 1",
            "My workflow description.",
            DateTime.UtcNow);

        // Act
        workflow.UpdateDescription(null);

        // Assert
        Assert.Null(workflow.Description);
    }
}