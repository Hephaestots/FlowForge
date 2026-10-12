using System.Reflection;
using System.Text.RegularExpressions;
using FlowForge.Domain.Common;

namespace FlowForge.Domain.Tests.Common;

// Agnostic guard for the "type.rule" error-code convention (see CLAUDE.md, "Errors").
// Each type still owns its errors; this only checks the shape of every code in the Domain assembly,
// so a typo like "handler_key.tooLong" or a copy-pasted code fails here instead of shipping.
public sealed partial class ErrorCodeConventionTests
{
    [GeneratedRegex(@"^[a-z]+(_[a-z]+)*\.[a-z]+(_[a-z]+)*\z", RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();

    private static readonly Type[] ErrorClasses =
        typeof(DomainError).Assembly.GetTypes()
            .Where(t => t is { Name: "Errors", DeclaringType: not null })
            .ToArray();

    public static TheoryData<string, string> Codes => ToTheoryData(e => e.Error.Code);

    public static TheoryData<string, string> Messages => ToTheoryData(e => e.Error.Message);

    [Theory]
    [InlineData("StepId")]
    [InlineData("TransitionKey")]
    [InlineData("HandlerKey")]
    public void Discovery_ShouldFindTheNestedErrorsClassOfEachValueObject(string ownerName) =>
        Assert.Contains(ErrorClasses, t => t.DeclaringType!.Name == ownerName);

    [Theory]
    [MemberData(nameof(Codes))]
    public void Code_ShouldFollowTypeDotRuleConvention(string member, string code) =>
        Assert.True(
            CodePattern().IsMatch(code),
            $"{member} has code '{code}': expected lowercase 'type.rule', with underscores inside each part.");

    [Theory]
    [MemberData(nameof(Messages))]
    public void Message_ShouldNotBeBlank(string member, string message) =>
        Assert.False(string.IsNullOrWhiteSpace(message), $"{member} has a blank message.");

    [Fact]
    public void Codes_ShouldBeUnique()
    {
        var duplicates = Discover()
            .GroupBy(e => e.Error.Code)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key}: {string.Join(", ", g.Select(e => e.Member))}")
            .ToList();

        Assert.Empty(duplicates);
    }

    private static TheoryData<string, string> ToTheoryData(Func<ErrorEntry, string> selector)
    {
        var data = new TheoryData<string, string>();
        foreach (var entry in Discover()) data.Add(entry.Member, selector(entry));
        return data;
    }

    private static IEnumerable<ErrorEntry> Discover()
    {
        const BindingFlags flags =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        foreach (var type in ErrorClasses)
        {
            var owner = type.DeclaringType!.Name;

            foreach (var field in type.GetFields(flags).Where(f => f.FieldType == typeof(DomainError)))
                yield return new ErrorEntry($"{owner}.Errors.{field.Name}", (DomainError)field.GetValue(null)!);

            foreach (var property in type.GetProperties(flags).Where(p => p.PropertyType == typeof(DomainError)))
                yield return new ErrorEntry($"{owner}.Errors.{property.Name}", (DomainError)property.GetValue(null)!);

            foreach (var method in type.GetMethods(flags).Where(m => m.ReturnType == typeof(DomainError)))
            {
                var parameters = method.GetParameters();

                // Fail loudly: silently skipping a factory would leave its code unchecked.
                if (parameters.Any(p => p.ParameterType != typeof(string)))
                    throw new InvalidOperationException(
                        $"{owner}.Errors.{method.Name} takes non-string parameters; teach the convention test how to call it.");

                var arguments = parameters.Select(_ => (object)"x").ToArray();
                yield return new ErrorEntry(
                    $"{owner}.Errors.{method.Name}(...)",
                    (DomainError)method.Invoke(null, arguments)!);
            }
        }
    }

    private sealed record ErrorEntry(string Member, DomainError Error);
}
