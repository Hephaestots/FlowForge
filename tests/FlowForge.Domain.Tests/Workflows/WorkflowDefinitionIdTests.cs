using FlowForge.Domain.Common;
using FlowForge.Domain.Workflows;

namespace FlowForge.Domain.Tests.Workflows;

public sealed class WorkflowDefinitionIdTests
{
    [Fact]
    public void New_ShouldReturnValidGuidV7()
    {
        // Act
        var workflowDefinitionId = WorkflowDefinitionId.New();

        // Assert
        Assert.NotEqual(Guid.Empty, workflowDefinitionId.Value);
        Assert.Equal(7, workflowDefinitionId.Value.Version);
    }

    [Fact]
    public void New_CalledTwice_ShouldReturnDifferentIds()
    {
        // Act
        var first = WorkflowDefinitionId.New();
        var second = WorkflowDefinitionId.New();

        // Assert
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void From_ShouldKeepTheGivenValue()
    {
        // Arrange
        var id = Guid.CreateVersion7();

        // Act
        var workflowDefinitionId = WorkflowDefinitionId.From(id);

        // Assert
        Assert.Equal(id, workflowDefinitionId.Value);
    }

    [Fact]
    public void From_WhenValueIsNotVersion7_ShouldAcceptIt()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var workflowDefinitionId = WorkflowDefinitionId.From(id);

        // Assert
        Assert.Equal(id, workflowDefinitionId.Value);
    }

    [Fact]
    public void From_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var first = WorkflowDefinitionId.From(id);
        var second = WorkflowDefinitionId.From(id);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void From_WhenValueIsEmpty_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => WorkflowDefinitionId.From(Guid.Empty));

        Assert.Equal(WorkflowDefinitionId.Errors.Empty.Code, ex.Error.Code);
    }
}
