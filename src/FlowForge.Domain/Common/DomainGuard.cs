namespace FlowForge.Domain.Common;

internal static class DomainGuard
{
    public static void NotNullOrWhiteSpace(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(
                "Value cannot be null or whitespace.", paramName);
    }
}