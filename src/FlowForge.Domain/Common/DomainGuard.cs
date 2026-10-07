using System.Runtime.CompilerServices;

namespace FlowForge.Domain.Common;

internal static class DomainGuard
{
    public static T AgainstNull<T>(
        T? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : class
    {
        return value ?? throw new DomainException($"{paramName} cannot be null.");
    }
    
    public static string AgainstNullOrWhiteSpace(
        string? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        return string.IsNullOrWhiteSpace(value) 
            ? throw new DomainException($"{paramName} cannot be null or whitespace.") 
            : value;
    }

    public static Guid AgainstEmpty(
        Guid value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        return value == Guid.Empty 
            ? throw new DomainException($"Unique identifiers [{paramName}] can't be empty.") 
            : value;
    }
}