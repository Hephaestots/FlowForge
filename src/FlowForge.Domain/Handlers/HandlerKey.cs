using FlowForge.Domain.Common;

namespace FlowForge.Domain.Handlers;

public sealed record HandlerKey
{
    private const int MaxLength = 100;

    public string Value { get; }

    private HandlerKey(string value) => Value = value;

    public static HandlerKey From(string? value)
    {
        value = DomainGuard.AgainstNullOrWhiteSpace(value, Errors.Empty);

        if (value.Length > MaxLength)
            throw new DomainException(Errors.TooLong);

        if (!char.IsAsciiLetterLower(value[0]))
            throw new DomainException(Errors.MustStartWithLetter);

        if (!value.All(c =>
                char.IsAsciiLetterLower(c) ||
                char.IsAsciiDigit(c) ||
                c == '-' ||
                c == '.'))
            throw new DomainException(Errors.InvalidCharacter);

        if (value.EndsWith('.'))
            throw new DomainException(Errors.TrailingSeparator);

        var segments = value.Split('.');

        if (segments.Any(string.IsNullOrEmpty))
            throw new DomainException(Errors.EmptySegment);

        if (segments.Any(seg => !char.IsAsciiLetterLower(seg[0])))
            throw new DomainException(Errors.SegmentMustStartWithLetter);

        if (segments.Any(seg => seg.Contains("--", StringComparison.Ordinal)))
            throw new DomainException(Errors.ConsecutiveHyphens);

        if (segments.Any(seg => seg.EndsWith('-')))
            throw new DomainException(Errors.TrailingHyphen);

        return new HandlerKey(value);
    }

    public static HandlerKey Restore(string? value) =>
        new(DomainGuard.AgainstNullOrWhiteSpace(value, Errors.Empty));

    public override string ToString() => Value;

    public static class Errors
    {
        public static readonly DomainError ConsecutiveHyphens =
            new("handler_key.consecutive_hyphens", "Handler key must not contain '--'.");

        public static readonly DomainError Empty =
            new("handler_key.empty", "Handler key must not be null or empty.");

        public static readonly DomainError EmptySegment =
            new("handler_key.empty_segment", "Handler key segment must not be empty.");

        public static readonly DomainError InvalidCharacter =
            new("handler_key.invalid_character", "Handler key may only contain lowercase letters, digits, '-' and '.'.");

        public static readonly DomainError MustStartWithLetter =
            new("handler_key.must_start_with_letter", "Handler key must start with a lowercase letter (a-z).");

        public static readonly DomainError SegmentMustStartWithLetter =
            new("handler_key.segment_must_start_with_letter", "Handler key segment must start with a lowercase letter (a-z).");

        public static readonly DomainError TooLong =
            new("handler_key.too_long", $"Handler key exceeds {MaxLength} characters.");

        public static readonly DomainError TrailingHyphen =
            new("handler_key.trailing_hyphen", "Handler key segment must not end with '-'.");

        public static readonly DomainError TrailingSeparator =
            new("handler_key.trailing_separator", "Handler key must not end with a '.'.");
    }
}