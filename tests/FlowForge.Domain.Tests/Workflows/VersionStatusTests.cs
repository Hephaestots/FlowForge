using FlowForge.Domain.Common;
using FlowForge.Domain.Workflows;

namespace FlowForge.Domain.Tests.Workflows;

public sealed class VersionStatusTests
{
    [Theory]
    [InlineData("Discarded")]
    [InlineData("Draft")]
    [InlineData("Published")]
    [InlineData("Retired")]
    public void FromName_WithKnownName_ShouldReturnMatchingStatus(string name)
    {
        // Act
        var status = VersionStatus.FromName(name);

        // Assert
        Assert.Equal(name, status.Name);
    }

    [Fact]
    public void FromName_WithKnownName_ShouldReturnTheSingletonInstance()
    {
        // Assert
        Assert.Same(VersionStatus.Draft, VersionStatus.FromName("Draft"));
        Assert.Same(VersionStatus.Published, VersionStatus.FromName("Published"));
        Assert.Same(VersionStatus.Retired, VersionStatus.FromName("Retired"));
        Assert.Same(VersionStatus.Discarded, VersionStatus.FromName("Discarded"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Unknown")]
    [InlineData("draft")]
    [InlineData("PUBLISHED")]
    [InlineData(" Draft ")]
    public void FromName_WithUnknownName_ShouldThrowDomainException(string name)
    {
        // Assert
        var ex = Assert.Throws<DomainException>(() => VersionStatus.FromName(name));

        Assert.Equal(VersionStatus.Errors.Unknown(name).Code, ex.Error.Code);
    }

    [Fact]
    public void FromName_WithNull_ShouldThrowDomainException()
    {
        // Assert
        var ex = Assert.Throws<DomainException>(() => VersionStatus.FromName(null!));

        Assert.Equal(VersionStatus.Errors.Unknown(null!), ex.Error);
    }

    [Fact]
    public void IsEditable_ShouldBeTrueOnlyForDraft()
    {
        // Assert
        Assert.True(VersionStatus.Draft.IsEditable);
        Assert.False(VersionStatus.Published.IsEditable);
        Assert.False(VersionStatus.Retired.IsEditable);
        Assert.False(VersionStatus.Discarded.IsEditable);
    }

    [Fact]
    public void CanStartInstance_ShouldBeTrueOnlyForPublished()
    {
        // Assert
        Assert.True(VersionStatus.Published.CanStartInstance);
        Assert.False(VersionStatus.Draft.CanStartInstance);
        Assert.False(VersionStatus.Retired.CanStartInstance);
        Assert.False(VersionStatus.Discarded.CanStartInstance);
    }

    [Theory]
    [MemberData(nameof(AllowedTransitions))]
    public void CanTransitionTo_WhenTransitionIsAllowed_ShouldBeTrue(VersionStatus from, VersionStatus to)
    {
        // Assert
        Assert.True(from.CanTransitionTo(to));
    }

    [Theory]
    [MemberData(nameof(ForbiddenTransitions))]
    public void CanTransitionTo_WhenTransitionIsForbidden_ShouldBeFalse(VersionStatus from, VersionStatus to)
    {
        // Assert
        Assert.False(from.CanTransitionTo(to));
    }

    [Fact]
    public void Discarded_ShouldBeTerminal()
    {
        // Arrange
        VersionStatus[] all = [VersionStatus.Discarded, VersionStatus.Draft, VersionStatus.Published, VersionStatus.Retired];

        // Assert
        Assert.All(all, next => Assert.False(VersionStatus.Discarded.CanTransitionTo(next)));
    }

    [Fact]
    public void Equals_WhenSameName_ShouldBeEqual()
    {
        // Assert
        Assert.Equal(VersionStatus.Draft, VersionStatus.FromName("Draft"));
        Assert.True(VersionStatus.Draft == VersionStatus.FromName("Draft"));
    }

    [Fact]
    public void Equals_WhenDifferentName_ShouldNotBeEqual()
    {
        // Assert
        Assert.NotEqual(VersionStatus.Draft, VersionStatus.Published);
        Assert.True(VersionStatus.Draft != VersionStatus.Published);
    }

    [Theory]
    [InlineData("Discarded")]
    [InlineData("Draft")]
    [InlineData("Published")]
    [InlineData("Retired")]
    public void ToString_ShouldReturnName(string name)
    {
        // Assert
        Assert.Equal(name, VersionStatus.FromName(name).ToString());
    }

    public static TheoryData<VersionStatus, VersionStatus> AllowedTransitions => new()
    {
        { VersionStatus.Draft, VersionStatus.Published },
        { VersionStatus.Draft, VersionStatus.Discarded },
        { VersionStatus.Published, VersionStatus.Retired },
        { VersionStatus.Retired, VersionStatus.Published },
    };

    public static TheoryData<VersionStatus, VersionStatus> ForbiddenTransitions => new()
    {
        // Self-transitions
        { VersionStatus.Draft, VersionStatus.Draft },
        { VersionStatus.Published, VersionStatus.Published },
        { VersionStatus.Retired, VersionStatus.Retired },
        { VersionStatus.Discarded, VersionStatus.Discarded },

        // Draft
        { VersionStatus.Draft, VersionStatus.Retired },

        // Published
        { VersionStatus.Published, VersionStatus.Draft },
        { VersionStatus.Published, VersionStatus.Discarded },

        // Retired
        { VersionStatus.Retired, VersionStatus.Draft },
        { VersionStatus.Retired, VersionStatus.Discarded },

        // Discarded is terminal
        { VersionStatus.Discarded, VersionStatus.Draft },
        { VersionStatus.Discarded, VersionStatus.Published },
        { VersionStatus.Discarded, VersionStatus.Retired },
    };
}
