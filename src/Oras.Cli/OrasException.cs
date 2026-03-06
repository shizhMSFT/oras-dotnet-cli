namespace Oras;

/// <summary>
/// Base exception for ORAS CLI errors with user-friendly messages.
/// </summary>
internal class OrasException : Exception
{
    public string? Recommendation { get; }

    public OrasException(string message, string? recommendation = null)
        : base(message)
    {
        Recommendation = recommendation;
    }

    public OrasException(string message, Exception innerException, string? recommendation = null)
        : base(message, innerException)
    {
        Recommendation = recommendation;
    }
}

/// <summary>
/// Exception for authentication/credential errors.
/// </summary>
internal class OrasAuthenticationException : OrasException
{
    public OrasAuthenticationException(string message, string? recommendation = null)
        : base(message, recommendation ?? "Check your credentials or run 'oras login' to authenticate.")
    {
    }
}

/// <summary>
/// Exception for network/connectivity errors.
/// </summary>
internal class OrasNetworkException : OrasException
{
    public OrasNetworkException(string message, Exception innerException, string? recommendation = null)
        : base(message, innerException, recommendation ?? "Check your network connection and registry address.")
    {
    }
}

/// <summary>
/// Exception for command usage errors.
/// </summary>
internal class OrasUsageException : OrasException
{
    public OrasUsageException(string message, string? recommendation = null)
        : base(message, recommendation)
    {
    }
}
