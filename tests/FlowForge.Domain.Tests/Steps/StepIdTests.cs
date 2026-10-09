using FlowForge.Domain.Common;
using FlowForge.Domain.Steps;

namespace FlowForge.Domain.Tests.Steps;

public static class StepIdTests
{
    public sealed class FromTests
    {
        [Theory]
        [MemberData(nameof(StepIdCases.ValidIds), MemberType = typeof(StepIdCases))]
        public void From_ValidId_ShouldPreserveValue(string value) =>
            Assert.Equal(value, StepId.From(value).Value);

        [Fact]
        public void From_ExactlyMaxLength_ShouldSucceed() =>
            Assert.Equal(64, StepId.From(new string('a', 64)).Value.Length);

        [Fact]
        public void From_MaxLengthWithAllAllowedCharacterKinds_ShouldSucceed()
        {
            var value = "a" + string.Concat(Enumerable.Repeat("1-b", 21)); // 1 + 63 = 64
            Assert.Equal(64, value.Length);

            Assert.Equal(value, StepId.From(value).Value);
        }

        [Fact]
        public void From_OverMaxLength_ShouldThrowWithTooLongCode() =>
            AssertFailsWith(StepId.Errors.TooLong.Code, new string('a', 65));

        [Fact]
        public void From_Null_ShouldThrowWithEmptyCode() =>
            AssertFailsWith(StepId.Errors.Empty.Code, null);

        [Fact]
        public void From_EmptyString_ShouldThrowWithEmptyCode() =>
            AssertFailsWith(StepId.Errors.Empty.Code, string.Empty);

        [Theory]
        [MemberData(nameof(StepIdCases.WhiteSpaceValues), MemberType = typeof(StepIdCases))]
        public void From_WhiteSpace_ShouldThrowWithEmptyCode(string value) =>
            AssertFailsWith(StepId.Errors.Empty.Code, value);

        [Theory]
        [MemberData(nameof(StepIdCases.InvalidFormatCases), MemberType = typeof(StepIdCases))]
        public void From_InvalidFormat_ShouldThrowWithExpectedCode(string expectedCode, string value) =>
            AssertFailsWith(expectedCode, value);

        [Theory]
        [MemberData(nameof(StepIdCases.PrecedenceCases), MemberType = typeof(StepIdCases))]
        public void From_MultipleViolations_ShouldReportFirstRuleInOrder(string expectedCode, string value) =>
            AssertFailsWith(expectedCode, value);

        private static void AssertFailsWith(string expectedCode, string? value)
        {
            var ex = Assert.Throws<DomainException>(() => StepId.From(value));

            Assert.Equal(expectedCode, ex.Error.Code);
        }
    }

    public sealed class RestoreTests
    {
        [Fact]
        public void Restore_ValidId_ShouldPreserveValue() =>
            Assert.Equal("approve", StepId.Restore("approve").Value);

        [Fact]
        public void Restore_OverMaxLength_ShouldPreserveValue() =>
            Assert.Equal(65, StepId.Restore(new string('a', 65)).Value.Length);

        [Theory]
        [MemberData(nameof(StepIdCases.InvalidIds), MemberType = typeof(StepIdCases))]
        public void Restore_InvalidId_ShouldPreserveValue(string value) =>
            Assert.Equal(value, StepId.Restore(value).Value);

        [Fact]
        public void Restore_Null_ShouldThrowWithEmptyCode() =>
            AssertFailsWith(StepId.Errors.Empty.Code, null);

        [Fact]
        public void Restore_EmptyString_ShouldThrowWithEmptyCode() =>
            AssertFailsWith(StepId.Errors.Empty.Code, string.Empty);

        [Theory]
        [MemberData(nameof(StepIdCases.WhiteSpaceValues), MemberType = typeof(StepIdCases))]
        public void Restore_WhiteSpace_ShouldThrowWithEmptyCode(string value) =>
            AssertFailsWith(StepId.Errors.Empty.Code, value);

        private static void AssertFailsWith(string expectedCode, string? value)
        {
            var ex = Assert.Throws<DomainException>(() => StepId.Restore(value));

            Assert.Equal(expectedCode, ex.Error.Code);
        }
    }

    public sealed class EqualityTests
    {
        [Fact]
        public void Equals_SameValue_ShouldBeEqual()
        {
            var first = StepId.From("a");
            var second = StepId.From("a");

            Assert.Equal(first, second);
            Assert.True(first == second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
        }

        [Fact]
        public void Equals_DifferentValue_ShouldNotBeEqual()
        {
            var first = StepId.From("a");
            var second = StepId.From("b");

            Assert.NotEqual(first, second);
            Assert.True(first != second);
        }

        [Fact]
        public void Equals_FromAndRestoreWithSameValue_ShouldBeEqualWithSameHash()
        {
            var strict = StepId.From("approve");
            var restored = StepId.Restore("approve");

            Assert.Equal(strict, restored);
            Assert.Equal(strict.GetHashCode(), restored.GetHashCode());
        }

        [Fact]
        public void Equals_RestoredValuesDifferingOnlyByCase_ShouldNotBeEqual() =>
            Assert.NotEqual(StepId.Restore("Approve"), StepId.Restore("approve"));

        [Fact]
        public void HashSet_ShouldFindEqualIdBuiltThroughOtherEntryPoint()
        {
            var set = new HashSet<StepId> { StepId.From("approve") };

            Assert.Contains(StepId.Restore("approve"), set);
        }
    }

    public sealed class ToStringTests
    {
        [Theory]
        [InlineData("a")]
        [InlineData("approve")]
        [InlineData("a1-b2-c3")]
        public void ToString_ShouldReturnValue(string value) =>
            Assert.Equal(value, StepId.From(value).ToString());

        [Fact]
        public void ToString_OnRestoredId_ShouldReturnValueUnchanged() =>
            Assert.Equal(" Approve ", StepId.Restore(" Approve ").ToString());
    }
}

public static class StepIdCases
{
    private static readonly (string Code, string Input)[] InvalidFormatRows =
    [
        (StepId.Errors.MustStartWithLetter.Code, "1abc"),
        (StepId.Errors.MustStartWithLetter.Code, "-abc"),
        (StepId.Errors.MustStartWithLetter.Code, "Approve"),    // uppercase first letter
        (StepId.Errors.MustStartWithLetter.Code, " approve "),  // leading space
        (StepId.Errors.InvalidCharacter.Code, "aPprove"),       // uppercase mid-string
        (StepId.Errors.InvalidCharacter.Code, "approvE"),       // uppercase at the end
        (StepId.Errors.InvalidCharacter.Code, "stép"),
        (StepId.Errors.InvalidCharacter.Code, "step_1"),
        (StepId.Errors.InvalidCharacter.Code, "a.b"),
        (StepId.Errors.InvalidCharacter.Code, "a b"),           // inner space
        (StepId.Errors.InvalidCharacter.Code, "approve\n"),     // trailing newline
        (StepId.Errors.InvalidCharacter.Code, "a٣"),            // Arabic-Indic digit: ASCII only
        (StepId.Errors.ConsecutiveHyphens.Code, "a--b"),
        (StepId.Errors.TrailingHyphen.Code, "abc-"),
    ];

    public static TheoryData<string> ValidIds =>
    [
        "a",
        "approve",
        "step-1",
        "a1-b2-c3",
        "a1",
        "a-b",
        "a-1",
    ];

    public static TheoryData<string> WhiteSpaceValues =>
    [
        " ",
        "   ",
        "\n",
        "\t",
    ];

    public static TheoryData<string, string> InvalidFormatCases
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var (code, input) in InvalidFormatRows) data.Add(code, input);
            return data;
        }
    }

    public static TheoryData<string> InvalidIds
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var (_, input) in InvalidFormatRows) data.Add(input);
            return data;
        }
    }

    // Inputs that break several rules at once: pins the order in which From checks them.
    public static TheoryData<string, string> PrecedenceCases => new()
    {
        { StepId.Errors.TooLong.Code, new string('A', 65) },     // too long + uppercase: length wins
        { StepId.Errors.MustStartWithLetter.Code, "1--" },       // bad start + hyphens
        { StepId.Errors.InvalidCharacter.Code, "aB--" },         // bad character + hyphens
        { StepId.Errors.ConsecutiveHyphens.Code, "a--" },        // consecutive + trailing: consecutive wins
    };
}
