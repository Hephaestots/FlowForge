using FlowForge.Domain.Common;
using FlowForge.Domain.Handlers;

namespace FlowForge.Domain.Tests.Handlers;

public static class HandlerKeyTests
{
    public sealed class FromTests
    {
        [Theory]
        [MemberData(nameof(HandlerKeyCases.ValidKeys), MemberType = typeof(HandlerKeyCases))]
        public void From_ValidKey_ShouldPreserveValue(string value) =>
            Assert.Equal(value, HandlerKey.From(value).Value);

        [Fact]
        public void From_ExactlyMaxLength_ShouldSucceed() =>
            Assert.Equal(100, HandlerKey.From(new string('a', 100)).Value.Length);

        [Fact]
        public void From_MaxLengthWithAllAllowedCharacterKinds_ShouldSucceed()
        {
            var value = "a." + string.Concat(Enumerable.Repeat("a-1.", 24)) + "cd";
            Assert.Equal(100, value.Length);

            Assert.Equal(value, HandlerKey.From(value).Value);
        }

        [Fact]
        public void From_OverMaxLength_ShouldThrowWithTooLongCode() =>
            AssertFailsWith(HandlerKey.Errors.TooLong.Code, new string('a', 101));

        [Fact]
        public void From_Null_ShouldThrowWithEmptyCode() =>
            AssertFailsWith(HandlerKey.Errors.Empty.Code, null);

        [Fact]
        public void From_EmptyString_ShouldThrowWithEmptyCode() =>
            AssertFailsWith(HandlerKey.Errors.Empty.Code, string.Empty);

        [Theory]
        [MemberData(nameof(HandlerKeyCases.WhiteSpaceValues), MemberType = typeof(HandlerKeyCases))]
        public void From_WhiteSpace_ShouldThrowWithEmptyCode(string value) =>
            AssertFailsWith(HandlerKey.Errors.Empty.Code, value);

        [Theory]
        [MemberData(nameof(HandlerKeyCases.InvalidFormatCases), MemberType = typeof(HandlerKeyCases))]
        public void From_InvalidFormat_ShouldThrowWithExpectedCode(string expectedCode, string value) =>
            AssertFailsWith(expectedCode, value);

        [Theory]
        [MemberData(nameof(HandlerKeyCases.PrecedenceCases), MemberType = typeof(HandlerKeyCases))]
        public void From_MultipleViolations_ShouldReportFirstRuleInOrder(string expectedCode, string value) =>
            AssertFailsWith(expectedCode, value);

        private static void AssertFailsWith(string expectedCode, string? value)
        {
            var ex = Assert.Throws<DomainException>(() => HandlerKey.From(value));

            Assert.Equal(expectedCode, ex.Error.Code);
        }
    }

    public sealed class RestoreTests
    {
        [Fact]
        public void Restore_ValidKey_ShouldPreserveValue() =>
            Assert.Equal("billing.send-email", HandlerKey.Restore("billing.send-email").Value);

        [Fact]
        public void Restore_OverMaxLength_ShouldPreserveValue() =>
            Assert.Equal(101, HandlerKey.Restore(new string('a', 101)).Value.Length);

        [Theory]
        [MemberData(nameof(HandlerKeyCases.InvalidKeys), MemberType = typeof(HandlerKeyCases))]
        public void Restore_InvalidKey_ShouldPreserveValue(string value) =>
            Assert.Equal(value, HandlerKey.Restore(value).Value);

        [Fact]
        public void Restore_Null_ShouldThrowWithEmptyCode() =>
            AssertFailsWith(HandlerKey.Errors.Empty.Code, null);

        [Fact]
        public void Restore_EmptyString_ShouldThrowWithEmptyCode() =>
            AssertFailsWith(HandlerKey.Errors.Empty.Code, string.Empty);

        [Theory]
        [MemberData(nameof(HandlerKeyCases.WhiteSpaceValues), MemberType = typeof(HandlerKeyCases))]
        public void Restore_WhiteSpace_ShouldThrowWithEmptyCode(string value) =>
            AssertFailsWith(HandlerKey.Errors.Empty.Code, value);

        private static void AssertFailsWith(string expectedCode, string? value)
        {
            var ex = Assert.Throws<DomainException>(() => HandlerKey.Restore(value));

            Assert.Equal(expectedCode, ex.Error.Code);
        }
    }

    public sealed class EqualityTests
    {
        [Fact]
        public void Equals_SameValue_ShouldBeEqual()
        {
            var first = HandlerKey.From("billing.send-email");
            var second = HandlerKey.From("billing.send-email");

            Assert.Equal(first, second);
            Assert.True(first == second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
        }

        [Fact]
        public void Equals_DifferentValue_ShouldNotBeEqual()
        {
            var first = HandlerKey.From("billing.charge-card");
            var second = HandlerKey.From("billing.send-email");

            Assert.NotEqual(first, second);
            Assert.True(first != second);
        }

        [Fact]
        public void Equals_FromAndRestoreWithSameValue_ShouldBeEqualWithSameHash()
        {
            var strict = HandlerKey.From("billing.send-email");
            var restored = HandlerKey.Restore("billing.send-email");

            Assert.Equal(strict, restored);
            Assert.Equal(strict.GetHashCode(), restored.GetHashCode());
        }

        [Fact]
        public void Equals_RestoredValuesDifferingOnlyByCase_ShouldNotBeEqual() =>
            Assert.NotEqual(HandlerKey.Restore("Billing.Send-Email"), HandlerKey.Restore("billing.send-email"));

        [Fact]
        public void HashSet_ShouldFindEqualKeyBuiltThroughOtherEntryPoint()
        {
            var set = new HashSet<HandlerKey> { HandlerKey.From("billing.send-email") };

            Assert.Contains(HandlerKey.Restore("billing.send-email"), set);
        }
    }

    public sealed class ToStringTests
    {
        [Theory]
        [InlineData("a")]
        [InlineData("billing.charge-card")]
        [InlineData("billing.send-email")]
        public void ToString_ShouldReturnValue(string value) =>
            Assert.Equal(value, HandlerKey.From(value).ToString());

        [Fact]
        public void ToString_OnRestoredKey_ShouldReturnValueUnchanged() =>
            Assert.Equal(" Billing.send-email ", HandlerKey.Restore(" Billing.send-email ").ToString());
    }
}

public static class HandlerKeyCases
{
    private static readonly (string Code, string Input)[] InvalidFormatRows =
    [
        (HandlerKey.Errors.MustStartWithLetter.Code, "1handler"),
        (HandlerKey.Errors.MustStartWithLetter.Code, "-handler"),
        (HandlerKey.Errors.MustStartWithLetter.Code, ".handler"),
        (HandlerKey.Errors.MustStartWithLetter.Code, "Handler"),
        (HandlerKey.Errors.InvalidCharacter.Code, "handler_key"),
        (HandlerKey.Errors.InvalidCharacter.Code, "handler key"),
        (HandlerKey.Errors.InvalidCharacter.Code, "handler@key"),
        (HandlerKey.Errors.InvalidCharacter.Code, "handlerKey"),
        (HandlerKey.Errors.InvalidCharacter.Code, "handlerékey"),
        (HandlerKey.Errors.TrailingSeparator.Code, "handler."),
        (HandlerKey.Errors.TrailingSeparator.Code, "namespace.handler."),
        (HandlerKey.Errors.EmptySegment.Code, "handler..execute"),
        (HandlerKey.Errors.EmptySegment.Code, "a..b"),
        (HandlerKey.Errors.SegmentMustStartWithLetter.Code, "handler.1execute"),
        (HandlerKey.Errors.SegmentMustStartWithLetter.Code, "handler.-execute"),
        (HandlerKey.Errors.ConsecutiveHyphens.Code, "handler--execute"),
        (HandlerKey.Errors.ConsecutiveHyphens.Code, "handler.execute--now"),
        (HandlerKey.Errors.TrailingHyphen.Code, "handler-"),
        (HandlerKey.Errors.TrailingHyphen.Code, "handler.execute-"),
        (HandlerKey.Errors.TrailingHyphen.Code, "a-.b"),
        (HandlerKey.Errors.TooLong.Code, new string('a', 101))
    ];

    private static readonly (string Code, string Input)[] LegacyOnlyRows =
    [
        (HandlerKey.Errors.MustStartWithLetter.Code, " handler"),
        (HandlerKey.Errors.MustStartWithLetter.Code, " handler "),
        (HandlerKey.Errors.InvalidCharacter.Code,    "handler "),
        (HandlerKey.Errors.InvalidCharacter.Code,    "handler\n"),
        (HandlerKey.Errors.InvalidCharacter.Code,    "handler\texecute"),
        (HandlerKey.Errors.MustStartWithLetter.Code, "Billing.Charge-Card"),
    ];

    public static TheoryData<string, string> LegacyFormatCases
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var (code, input) in InvalidFormatRows.Concat(LegacyOnlyRows))
                data.Add(code, input);
            return data;
        }
    }

    public static TheoryData<string> ValidKeys =>
    [
        "a",
        "a.b.c",
        "a1.b2",
        "a-b.c-d",
        "send-email",
        "billing.charge-card" // headline example.
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
        { HandlerKey.Errors.TooLong.Code, new string('A', 101) },     // too long + uppercase: length wins
        { HandlerKey.Errors.TooLong.Code, new string('.', 101) },     // too long + separators: length wins
        { HandlerKey.Errors.MustStartWithLetter.Code, "1--" },                  // bad start + hyphens
        { HandlerKey.Errors.MustStartWithLetter.Code, ".a." },                  // bad start + trailing separator: starts-with wins.
        { HandlerKey.Errors.InvalidCharacter.Code, "aB.--" },                   // bad character + separator + hyphens
        { HandlerKey.Errors.TrailingSeparator.Code, "a--." },                   // consecutive + trailing separator: trailing wins
        { HandlerKey.Errors.TrailingSeparator.Code, "a.-." },                   // trailing separator + segment-start: trailing wins
        { HandlerKey.Errors.EmptySegment.Code, "a2..a--"},                      // valid first segment + empty segment + consecutive hyphens: empty segment wins.
        { HandlerKey.Errors.EmptySegment.Code, "a..-"},                         // valid first segment + empty segment + trailing hyphen: empty segment wins.
        { HandlerKey.Errors.SegmentMustStartWithLetter.Code, "a1--b2.2fa" },    // invalid first segment with consecutive hyphens + segment starts with number: segment `starts with` wins.
        { HandlerKey.Errors.ConsecutiveHyphens.Code, "a1--b2.c3-"},             // consecutive + trailing '-': consecutive wins.
        { HandlerKey.Errors.TrailingHyphen.Code, "a1-b2.c3-"}
    };
}