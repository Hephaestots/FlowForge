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
            new DisplayName("  My workflow  "),
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
        var first = WorkflowDefinition.Create(new DisplayName("workflow 1"), null, DateTimeOffset.UtcNow);
        var second = WorkflowDefinition.Create(new DisplayName("workflow 1"), null, DateTimeOffset.UtcNow);

        // Assert
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Create_WithNonUtcOffset_ShouldNormalizeCreatedAtToUtc()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 10, 7, 12, 0, 0, TimeSpan.FromHours(2));

        // Act
        var workflow = WorkflowDefinition.Create(new DisplayName("My workflow"), null, createdAt);

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
        var workflow = WorkflowDefinition.Rehydrate(WorkflowDefinitionId.New(), new DisplayName("My workflow"), null, createdAt);

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
            new DisplayName("My workflow"),
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
            new DisplayName("My workflow"),
            null,
            DateTimeOffset.UtcNow));

        Assert.Equal(WorkflowDefinition.Errors.Null.Code, ex.Error.Code);
    }

    [Fact]
    public void Rename_WithValidName_ShouldRenameWorkflowDefinition()
    {
        // Arrange
        var workflow = WorkflowDefinition.Create(new DisplayName("Old name"), null, DateTimeOffset.UtcNow);

        // Act
        workflow.Rename(new DisplayName("  New name  "));

        // Assert
        Assert.Equal("New name", workflow.Name.Value);
    }

    [Fact]
    public void UpdateDescription_WhenDescriptionIsValidString_ShouldUpdateWorkflowDefinitionDescription()
    {
        // Arrange
        var workflow = WorkflowDefinition.Create(new DisplayName("workflow 1"), null, DateTimeOffset.UtcNow);

        // Act
        workflow.UpdateDescription("My workflow description.");

        // Assert
        Assert.Equal("My workflow description.", workflow.Description);
    }

    [Fact]
    public void UpdateDescription_WhenDescriptionIsNull_ShouldSetEmptyDescription()
    {
        // Arrange
        var workflow = WorkflowDefinition.Create(
            new DisplayName("workflow 1"),
            "My workflow description.",
            DateTimeOffset.UtcNow);

        // Act
        workflow.UpdateDescription(null);

        // Assert
        Assert.Equal(string.Empty, workflow.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Create_WhenDescriptionIsNullOrWhiteSpace_ShouldSetEmptyDescription(string? description)
    {
        var workflow = WorkflowDefinition.Create(new DisplayName("workflow 1"), description, DateTimeOffset.UtcNow);

        Assert.Equal(string.Empty, workflow.Description);
    }

    [Fact]
    public void Create_ShouldTrimDescriptionAndPreserveCase()
    {
        var workflow = WorkflowDefinition.Create(new DisplayName("workflow 1"), "  Mixed CASE text  ", DateTimeOffset.UtcNow);

        Assert.Equal("Mixed CASE text", workflow.Description);
    }

    [Fact]
    public void Create_WhenDescriptionIsExactlyMaxLength_ShouldSucceed()
    {
        var description = new string('a', WorkflowDefinition.MaxDescriptionLength);

        var workflow = WorkflowDefinition.Create(new DisplayName("workflow 1"), description, DateTimeOffset.UtcNow);

        Assert.Equal(description, workflow.Description);
    }

    [Fact]
    public void Create_WhenDescriptionExceedsMaxLength_ShouldThrowDomainException()
    {
        var description = new string('a', WorkflowDefinition.MaxDescriptionLength + 1);

        var ex = Assert.Throws<DomainException>(() =>
            WorkflowDefinition.Create(new DisplayName("workflow 1"), description, DateTimeOffset.UtcNow));

        Assert.Equal(WorkflowDefinition.Errors.DescriptionTooLong.Code, ex.Error.Code);
    }

    [Fact]
    public void Create_WhenPaddingPushesRawLengthOverMax_ShouldMeasureTrimmedLength()
    {
        var description = "  " + new string('a', WorkflowDefinition.MaxDescriptionLength) + "  ";

        var workflow = WorkflowDefinition.Create(new DisplayName("workflow 1"), description, DateTimeOffset.UtcNow);

        Assert.Equal(WorkflowDefinition.MaxDescriptionLength, workflow.Description.Length);
    }

    [Fact]
    public void Create_WhenNameIsNull_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            WorkflowDefinition.Create(null!, null, DateTimeOffset.UtcNow));

        Assert.Equal(WorkflowDefinition.Errors.NameNull.Code, ex.Error.Code);
    }

    [Fact]
    public void Rename_WhenNameIsNull_ShouldThrowDomainException()
    {
        var workflow = WorkflowDefinition.Create(new DisplayName("workflow 1"), null, DateTimeOffset.UtcNow);

        var ex = Assert.Throws<DomainException>(() => workflow.Rename(null!));

        Assert.Equal(WorkflowDefinition.Errors.NameNull.Code, ex.Error.Code);
    }

    [Fact]
    public void UpdateDescription_WhenDescriptionExceedsMaxLength_ShouldThrowAndKeepPreviousValue()
    {
        var workflow = WorkflowDefinition.Create(new DisplayName("workflow 1"), "original", DateTimeOffset.UtcNow);

        var ex = Assert.Throws<DomainException>(() =>
            workflow.UpdateDescription(new string('a', WorkflowDefinition.MaxDescriptionLength + 1)));

        Assert.Equal(WorkflowDefinition.Errors.DescriptionTooLong.Code, ex.Error.Code);
        Assert.Equal("original", workflow.Description);
    }

    [Fact]
    public void UpdateDescription_ShouldTrimDescription()
    {
        var workflow = WorkflowDefinition.Create(new DisplayName("workflow 1"), null, DateTimeOffset.UtcNow);

        workflow.UpdateDescription("  trimmed  ");

        Assert.Equal("trimmed", workflow.Description);
    }

    [Fact]
    public void Rehydrate_WhenDescriptionExceedsMaxLength_ShouldStillLoad()
    {
        var description = new string('a', WorkflowDefinition.MaxDescriptionLength + 50);

        var workflow = WorkflowDefinition.Rehydrate(
            WorkflowDefinitionId.New(), new DisplayName("workflow 1"), description, DateTimeOffset.UtcNow);

        Assert.Equal(description, workflow.Description);
    }

    [Fact]
    public void Rehydrate_WhenDescriptionIsNull_ShouldSetEmptyDescription()
    {
        var workflow = WorkflowDefinition.Rehydrate(
            WorkflowDefinitionId.New(), new DisplayName("workflow 1"), null, DateTimeOffset.UtcNow);

        Assert.Equal(string.Empty, workflow.Description);
    }
}
