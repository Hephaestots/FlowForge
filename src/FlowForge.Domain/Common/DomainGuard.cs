using System.Runtime.CompilerServices;

namespace FlowForge.Domain.Common;

internal static class DomainGuard
{
    public static string AgainstNullOrWhiteSpace(
        string? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        return string.IsNullOrWhiteSpace(value) 
            ? throw new DomainException("Value cannot be null or whitespace.") 
            : value;
    }

    public static Guid AgainstEmpty(
        Guid value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        return value == Guid.Empty 
            ? throw new DomainException("Unique identifiers can't be empty.") 
            : value;
    }
}