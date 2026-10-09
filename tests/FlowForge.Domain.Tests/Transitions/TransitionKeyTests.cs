using FlowForge.Domain.Common;
using FlowForge.Domain.Transitions;

namespace FlowForge.Domain.Tests.Transitions;

public static class TransitionKeyTests
{
    public sealed class FromTests
    {
        [Theory]
        [MemberData(nameof(TransitionKeyCases.ValidKeys), MemberType = typeof(TransitionKeyCases))]
        public void From_ValidKey_ShouldPreserveValue(string value) =>
            Assert.Equal(value, TransitionKey.From(value).Value);

        [Fact]
        public void From_ExactlyMaxLength_ShouldSucceed() =>
            Assert.Equal(50, TransitionKey.From(new string('a', 50)).Value.Length);

        [Fact]
        public void From_MaxLengthWithAllAllowedCharacterKinds_ShouldSucceed()
        {
            var value = "a" + string.Concat(Enumerable.Repeat("1-b", 16)) + "c"; // 1 + 48 + 1 = 50
            Assert.Equal(50, value.Length);

            Assert.Equal(value, TransitionKey.From(value).Value);
        }

        [Fact]
        public void From_OverMaxLength_ShouldThrowWithTooLongCode() =>
            AssertFailsWith(TransitionKey.Errors.TooLong.Code, new string('a', 51));

        [Fact]
        public void From_Null_ShouldThrowWithEmptyCode() =>
            AssertFailsWith(TransitionKey.Errors.Empty.Code, null);

        [Fact]
        public void From_EmptyString_ShouldThrowWithEmptyCode() =>
            AssertFailsWith(TransitionKey.Errors.Empty.Code, string.Empty);

        [Theory]
        [MemberData(nameof(TransitionKeyCases.WhiteSpaceValues), MemberType = typeof(TransitionKeyCases))]
        public void From_WhiteSpace_ShouldThrowWithEmptyCode(string value) =>
            AssertFailsWith(TransitionKey.Errors.Empty.Code, value);

        [Theory]
        [MemberData(nameof(TransitionKeyCases.InvalidFormatCases), MemberType = typeof(TransitionKeyCases))]
        public void From_InvalidFormat_ShouldThrowWithExpectedCode(string expectedCode, string value) =>
            AssertFailsWith(expectedCode, value);

        [Theory]
        [MemberData(nameof(TransitionKeyCases.PrecedenceCases), MemberType = typeof(TransitionKeyCases))]
        public void From_MultipleViolations_ShouldReportFirstRuleInOrder(string expectedCode, string value) =>
            AssertFailsWith(expectedCode, value);

        private static void AssertFailsWith(string expectedCode, string? value)
        {
            var ex = Assert.Throws<DomainException>(() => TransitionKey.From(value));

            Assert.Equal(expectedCode, ex.Error.Code);
        }
    }

    public sealed class RestoreTests
    {
        [Fact]
        public void Restore_ValidKey_ShouldPreserveValue() =>
            Assert.Equal("approved", TransitionKey.Restore("approved").Value);

        [Fact]
        public void Restore_OverMaxLength_ShouldPreserveValue() =>
            Assert.Equal(51, TransitionKey.Restore(new string('a', 51)).Value.Length);

        [Theory]
        [MemberData(nameof(TransitionKeyCases.InvalidKeys), MemberType = typeof(TransitionKeyCases))]
        public void Restore_InvalidKey_ShouldPreserveValue(string value) =>
            Assert.Equal(value, TransitionKey.Restore(value).Value);

        [Fact]
        public void Restore_Null_ShouldThrowWithEmptyCode() =>
            AssertFailsWith(TransitionKey.Errors.Empty.Code, null);

        [Fact]
        public void Restore_EmptyString_ShouldThrowWithEmptyCode() =>
            AssertFailsWith(TransitionKey.Errors.Empty.Code, string.Empty);

        [Theory]
        [MemberData(nameof(TransitionKeyCases.WhiteSpaceValues), MemberType = typeof(TransitionKeyCases))]
        public void Restore_WhiteSpace_ShouldThrowWithEmptyCode(string value) =>
            AssertFailsWith(TransitionKey.Errors.Empty.Code, value);

        private static void AssertFailsWith(string expectedCode, string? value)
        {
            var ex = Assert.Throws<DomainException>(() => TransitionKey.Restore(value));

            Assert.Equal(expectedCode, ex.Error.Code);
        }
    }

    public sealed class EqualityTests
    {
        [Fact]
        public void Equals_SameValue_ShouldBeEqual()
        {
            var first = TransitionKey.From("approved");
            var second = TransitionKey.From("approved");

            Assert.Equal(first, second);
            Assert.True(first == second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
        }

        [Fact]
        public void Equals_DifferentValue_ShouldNotBeEqual()
        {
            var first = TransitionKey.From("approved");
            var second = TransitionKey.From("rejected");

            Assert.NotEqual(first, second);
            Assert.True(first != second);
        }

        [Fact]
        public void Equals_FromAndRestoreWithSameValue_ShouldBeEqualWithSameHash()
        {
            var strict = TransitionKey.From("approved");
            var restored = TransitionKey.Restore("approved");

            Assert.Equal(strict, restored);
            Assert.Equal(strict.GetHashCode(), restored.GetHashCode());
        }

        [Fact]
        public void Equals_RestoredValuesDifferingOnlyByCase_ShouldNotBeEqual() =>
            Assert.NotEqual(TransitionKey.Restore("Approved"), TransitionKey.Restore("approved"));

        [Fact]
        public void HashSet_ShouldFindEqualKeyBuiltThroughOtherEntryPoint()
        {
            var set = new HashSet<TransitionKey> { TransitionKey.From("approved") };

            Assert.Contains(TransitionKey.Restore("approved"), set);
        }
    }

    public sealed class ToStringTests
    {
        [Theory]
        [InlineData("a")]
        [InlineData("approved")]
        [InlineData("needs-info")]
        public void ToString_ShouldReturnValue(string value) =>
            Assert.Equal(value, TransitionKey.From(value).ToString());

        [Fact]
        public void ToString_OnRestoredKey_ShouldReturnValueUnchanged() =>
            Assert.Equal(" Approved ", TransitionKey.Restore(" Approved ").ToString());
    }
}

public static class TransitionKeyCases
{
    private static readonly (string Code, string Input)[] InvalidFormatRows =
    [
        (TransitionKey.Errors.MustStartWithLetter.Code, "1abc"),
        (TransitionKey.Errors.MustStartWithLetter.Code, "-abc"),
        (TransitionKey.Errors.MustStartWithLetter.Code, "Approved"),    // uppercase first letter
        (TransitionKey.Errors.MustStartWithLetter.Code, " approved "),  // leading space
        (TransitionKey.Errors.InvalidCharacter.Code, "aPproved"),       // uppercase mid-string
        (TransitionKey.Errors.InvalidCharacter.Code, "approveD"),       // uppercase at the end
        (TransitionKey.Errors.InvalidCharacter.Code, "appróved"),
        (TransitionKey.Errors.InvalidCharacter.Code, "needs_info"),
        (TransitionKey.Errors.InvalidCharacter.Code, "a.b"),
        (TransitionKey.Errors.InvalidCharacter.Code, "a b"),            // inner space
        (TransitionKey.Errors.InvalidCharacter.Code, "approved\n"),     // trailing newline
        (TransitionKey.Errors.InvalidCharacter.Code, "a٣"),             // Arabic-Indic digit: ASCII only
        (TransitionKey.Errors.ConsecutiveHyphens.Code, "a--b"),
        (TransitionKey.Errors.TrailingHyphen.Code, "abc-"),
    ];

    public static TheoryData<string> ValidKeys =>
    [
        "a",
        "approved",
        "needs-info",
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

    public static TheoryData<string> InvalidKeys
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
        { TransitionKey.Errors.TooLong.Code, new string('A', 51) },     // too long + uppercase: length wins
        { TransitionKey.Errors.MustStartWithLetter.Code, "1--" },       // bad start + hyphens
        { TransitionKey.Errors.InvalidCharacter.Code, "aB--" },         // bad character + hyphens
        { TransitionKey.Errors.ConsecutiveHyphens.Code, "a--" },        // consecutive + trailing: consecutive wins
    };
}
