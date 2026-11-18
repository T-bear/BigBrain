using System.Security.Cryptography;
using System.Text;

namespace BigBrain.Api;

/// <summary>
/// Finance HTTP boundary. An attacker-controlled IP, forwarded header, Docker gateway or
/// reverse proxy never grants access. Only the independently provisioned bearer token does.
/// </summary>
public sealed class FinanceAccessGuard
{
    private readonly byte[]? _requiredTokenHash;

    public FinanceAccessGuard(bool required, string? token)
    {
        if (!required) return;
        if (string.IsNullOrWhiteSpace(token) || token.Length < 48 || token.Length > 256)
            throw new InvalidOperationException("A strong Finance access token must be configured.");
        _requiredTokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
    }

    public bool Allows(string? authorization)
    {
        if (_requiredTokenHash is null) return true;
        if (authorization is null || !authorization.StartsWith("Bearer ", StringComparison.Ordinal) ||
            authorization.Length > 263) return false;
        var supplied = authorization["Bearer ".Length..];
        if (supplied.Length < 48 || supplied.Length > 256) return false;
        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(supplied));
        return CryptographicOperations.FixedTimeEquals(_requiredTokenHash, suppliedHash);
    }
}
