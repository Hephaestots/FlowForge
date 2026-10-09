using FlowForge.Domain.Common;
using FlowForge.Domain.Workflows;

namespace FlowForge.Domain.Tests.Workflows;

public sealed class WorkflowDefinitionTests
{
    [Fact]
    public void Create_WithValidValues_ShouldCreateWorkflowDefinition()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var workflow = WorkflowDefinition.Create(
            "  My workflow  ",
            "My workflow description",
            createdAt);

        // Assert
        Assert.Equal("My workflow", workflow.Name.Value);
        Assert.Equal("My workflow description", workflow.Description);
        Assert.Equal(createdAt, workflow.CreatedAt);
    }

    [Fact]
    public void Create_ShouldGenerateANewId()
    {
        // Act
        var first = WorkflowDefinition.Create("workflow 1", null, DateTimeOffset.UtcNow);
        var second = WorkflowDefinition.Create("workflow 1", null, DateTimeOffset.UtcNow);

        // Assert
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Create_WithNonUtcOffset_ShouldNormalizeCreatedAtToUtc()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 10, 7, 12, 0, 0, TimeSpan.FromHours(2));

        // Act
        var workflow = WorkflowDefinition.Create("My workflow", null, createdAt);

        // Assert
        Assert.Equal(TimeSpan.Zero, workflow.CreatedAt.Offset);
        Assert.Equal(new DateTimeOffset(2026, 10, 7, 10, 0, 0, TimeSpan.Zero), workflow.CreatedAt);
    }

    [Fact]
    public void Rehydrate_WithNonUtcOffset_ShouldNormalizeCreatedAtToUtc()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 10, 7, 12, 0, 0, TimeSpan.FromHours(-5));

        // Act
        var workflow = WorkflowDefinition.Rehydrate(WorkflowDefinitionId.New(), "My workflow", null, createdAt);

        // Assert
        Assert.Equal(TimeSpan.Zero, workflow.CreatedAt.Offset);
        Assert.Equal(new DateTimeOffset(2026, 10, 7, 17, 0, 0, TimeSpan.Zero), workflow.CreatedAt);
    }

    [Fact]
    public void Rehydrate_WithValidValues_ShouldRehydrateWorkflowDefinition()
    {
        // Arrange
        var id = WorkflowDefinitionId.New();
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var workflow = WorkflowDefinition.Rehydrate(
            id,
            "My workflow",
            "My workflow description",
            createdAt);

        // Assert
        Assert.Equal(id, workflow.Id);
        Assert.Equal("My workflow", workflow.Name.Value);
        Assert.Equal("My workflow description", workflow.Description);
        Assert.Equal(createdAt, workflow.CreatedAt);
    }

    [Fact]
    public void Rehydrate_WhenIdIsNull_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => WorkflowDefinition.Rehydrate(
            null!,
            "My workflow",
            null,
            DateTimeOffset.UtcNow));
        
        Assert.Equal(WorkflowDefinition.Errors.Null.Code, ex.Error.Code);
    }

    [Fact]
    public void Rename_WithValidName_ShouldRenameWorkflowDefinition()
    {
        // Arrange
        var workflow = WorkflowDefinition.Create("Old name", null, DateTimeOffset.UtcNow);

        // Act
        workflow.Rename("  New name  ");

        // Assert
        Assert.Equal("New name", workflow.Name.Value);
    }

    [Fact]
    public void UpdateDescription_WhenDescriptionIsValidString_ShouldUpdateWorkflowDefinitionDescription()
    {
        // Arrange
        var workflow = WorkflowDefinition.Create("workflow 1", null, DateTimeOffset.UtcNow);

        // Act
        workflow.UpdateDescription("My workflow description.");

        // Assert
        Assert.Equal("My workflow description.", workflow.Description);
    }

    [Fact]
    public void UpdateDescription_WhenDescriptionIsNull_ShouldClearWorkflowDefinitionDescription()
    {
        // Arrange
        var workflow = WorkflowDefinition.Create(
            "workflow 1",
            "My workflow description.",
            DateTimeOffset.UtcNow);

        // Act
        workflow.UpdateDescription(null);

        // Assert
        Assert.Null(workflow.Description);
    }
}
