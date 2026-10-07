using FlowForge.Domain.Common;
using FlowForge.Domain.Workflows;

namespace FlowForge.Domain.Tests;

public sealed class WorkflowTests
{
    [Fact]
    public void Constructor_WithValidValues_CreateWorkflow()
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdAtUtc = DateTime.UtcNow;
        
        // Act
        WorkflowDefinition workflow = new(
            id, 
            "My workflow", 
            "My workflow description", 
            createdAtUtc);
        
        // Assert
        Assert.Equal(id, workflow.Id);
        Assert.Equal("My workflow", workflow.Name);
        Assert.Equal("My workflow description", workflow.Description);
        Assert.Equal(createdAtUtc, workflow.CreatedAtUtc);
    }

    [Fact]
    public void Constructor_WithEmptyName_ThrowsArgumentException()
    {
        // Assert
        Assert.Throws<DomainException>(() => new WorkflowDefinition(
            Guid.NewGuid(),
            " ",
            null,
            DateTime.UtcNow));
    }

    [Fact]
    public void Rename_WithValidName_ShouldRenameWorkflow()
    {
        // Arrange
        var workflow = new WorkflowDefinition(
            Guid.NewGuid(),
            "Old name",
            null,
            DateTime.UtcNow);
        
        // Act
        workflow.Rename("New name");
        
        // Assert
        Assert.Equal("New name", workflow.Name);
    }

    [Fact]
    public void Rename_WhenNameIsEmpty_ShouldThrowArgumentException()
    {
        // Arrange
        var workflow = new WorkflowDefinition(
            Guid.NewGuid(),
            "Old name",
            null,
            DateTime.UtcNow);
        
        // Act
        // Assert
        Assert.Throws<DomainException>(() => workflow.Rename(" "));
    }

    [Fact]
    public void UpdateDescription_WhenDescriptionIsValidString_ShouldUpdateDescription()
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
    public void UpdateDescription_WhenDescriptionIsNull_ShouldClearDescription()
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