using FlowForge.Domain.Common;

namespace FlowForge.Domain.Tests.Common;

public sealed class DisplayNameTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\r\n")]
    public void Constructor_WhenValueIsNullOrWhiteSpace_ShouldThrowDomainException(string? value)
    {
        // Assert
        var ex = Assert.Throws<DomainException>(() => new DisplayName(value));

        Assert.Equal(DisplayName.Errors.Empty.Code, ex.Error.Code);
    }

    [Theory]
    [InlineData("My workflow", "My workflow")]
    [InlineData("   My workflow   ", "My workflow")]
    [InlineData("\tMy workflow\r\n", "My workflow")]
    [InlineData("My   workflow", "My   workflow")]
    public void Constructor_WithValidValue_ShouldTrimValue(string value, string expected)
    {
        // Act
        var displayName = new DisplayName(value);

        // Assert
        Assert.Equal(expected, displayName.Value);
    }

    [Theory]
    [InlineData("my workflow")]
    [InlineData("iOS release")]
    [InlineData("MY WORKFLOW")]
    public void Constructor_WithValidValue_ShouldPreserveCasing(string value)
    {
        // Act
        var displayName = new DisplayName(value);

        // Assert
        Assert.Equal(value, displayName.Value);
    }

    [Theory]
    [InlineData("My workflow", "my workflow")]
    [InlineData("My workflow", "MY WORKFLOW")]
    [InlineData("iOS release", "  IOS Release  ")]
    [InlineData("élan", "ÉLAN")]
    public void Equals_WhenValuesDifferOnlyByCasing_ShouldBeEqual(string first, string second)
    {
        // Arrange
        var firstName = new DisplayName(first);
        var secondName = new DisplayName(second);

        // Assert
        Assert.True(firstName.Equals(secondName));
        Assert.True(firstName == secondName);
        Assert.Equal(firstName.GetHashCode(), secondName.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var displayName = new DisplayName("My workflow");

        // Assert
        Assert.Equal("My workflow", displayName.ToString());
    }

    [Fact]
    public void Equals_WhenValuesMatchAfterTrimming_ShouldBeEqual()
    {
        // Arrange
        var first = new DisplayName("My workflow");
        var second = new DisplayName("  My workflow  ");

        // Assert
        Assert.Equal(first, second);
    }

    [Fact]
    public void Equals_WhenValuesDiffer_ShouldNotBeEqual()
    {
        // Arrange
        var first = new DisplayName("My workflow");
        var second = new DisplayName("Another workflow");

        // Assert
        Assert.NotEqual(first, second);
    }
}
