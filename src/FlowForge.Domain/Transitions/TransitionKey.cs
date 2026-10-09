using FlowForge.Domain.Common;

namespace FlowForge.Domain.Transitions;

public sealed record TransitionKey
{
    private const int MaxLength = 50;

    public string Value { get; }

    private TransitionKey(string value) => Value = value;

    public static TransitionKey From(string? value)
    {
        value = DomainGuard.AgainstNullOrWhiteSpace(value, Errors.Empty);

        if (value.Length > MaxLength)
            throw new DomainException(Errors.TooLong);

        if (!char.IsAsciiLetterLower(value[0]))
            throw new DomainException(Errors.MustStartWithLetter);

        if (!value.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-'))
            throw new DomainException(Errors.InvalidCharacter);

        if (value.Contains("--", StringComparison.Ordinal))
            throw new DomainException(Errors.ConsecutiveHyphens);

        if (value[^1] == '-')
            throw new DomainException(Errors.TrailingHyphen);

        return new TransitionKey(value);
    }

    public static TransitionKey Restore(string? value) =>
        new(DomainGuard.AgainstNullOrWhiteSpace(value, Errors.Empty));

    public override string ToString() => Value;

    public static class Errors
    {
        public static readonly DomainError Empty =
            new("transition_key.empty", "Transition key must not be null or empty.");

        public static readonly DomainError TooLong =
            new("transition_key.too_long", $"Transition key exceeds {MaxLength} characters.");

        public static readonly DomainError MustStartWithLetter =
            new("transition_key.must_start_with_letter", "Transition key must start with a lowercase letter (a-z).");

        public static readonly DomainError InvalidCharacter =
            new("transition_key.invalid_character", "Transition key may only contain lowercase letters, digits and '-'.");

        public static readonly DomainError ConsecutiveHyphens =
            new("transition_key.consecutive_hyphens", "Transition key must not contain '--'.");

        public static readonly DomainError TrailingHyphen =
            new("transition_key.trailing_hyphen", "Transition key must not end with '-'.");
    }
}