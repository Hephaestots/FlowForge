using System.Reflection;
using FlowForge.Domain.Steps;

namespace FlowForge.Domain.Tests.Common;

public sealed class ImmutableMembersTests
{
    [Theory]
    [InlineData(typeof(StepDefinition))]
    public void Type_ShouldNotExposeMutableMembers(Type type)
    {
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(prop => prop.Name != "EqualityContract")
            .ToArray();

        Assert.NotEmpty(props);

        Assert.All(props, p => Assert.True(
            p.SetMethod is null, $"{p.Name} has an accessible setter."));

        Assert.DoesNotContain(type.GetFields(BindingFlags.Public | BindingFlags.Instance), f => !f.IsInitOnly);
        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
    }
}