namespace FlowForge.Domain.Common;

public sealed record DisplayName
{
    public string Value { get; }

    public DisplayName(string? value) =>
        Value = DomainGuard.AgainstNullOrWhiteSpace(value).Trim();

    public bool Equals(DisplayName? other) =>
        other is not null && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    public override string ToString() => Value;
}