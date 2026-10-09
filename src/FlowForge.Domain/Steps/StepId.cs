using FlowForge.Domain.Common;

namespace FlowForge.Domain.Steps;

public sealed record StepId
{
    private const int MaxLength = 64;

    public string Value { get; }

    private StepId(string value) => Value = value;

    public static StepId From(string? value)
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

        return new StepId(value);
    }

    public static StepId Restore(string? value) =>
        new(DomainGuard.AgainstNullOrWhiteSpace(value, Errors.Empty));

    public override string ToString() => Value;


    public static class Errors
    {
        public static readonly DomainError Empty =
            new("step_id.empty", "Step id must not be null or empty.");

        public static readonly DomainError TooLong =
            new("step_id.too_long", $"Step id exceeds {MaxLength} characters.");

        public static readonly DomainError MustStartWithLetter =
            new("step_id.must_start_with_letter", "Step id must start with a lowercase letter (a-z).");

        public static readonly DomainError InvalidCharacter =
            new("step_id.invalid_character", "Step id may only contain lowercase letters, digits and '-'.");

        public static readonly DomainError ConsecutiveHyphens =
            new("step_id.consecutive_hyphens", "Step id must not contain '--'.");

        public static readonly DomainError TrailingHyphen =
            new("step_id.trailing_hyphen", "Step id must not end with '-'.");
    }
}