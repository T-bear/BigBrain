using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using BigBrain.Sentinel.Contracts;
using Microsoft.Extensions.Options;

namespace BigBrain.Sentinel;

public static class SentinelCertificateLoader
{
    public static X509Certificate2 LoadPkcs12(string path) =>
        X509CertificateLoader.LoadPkcs12FromFile(
            path,
            password: null,
            X509KeyStorageFlags.EphemeralKeySet);

    public static bool Matches(X509Certificate2? presented, X509Certificate2 expected) =>
        presented is not null
        && CryptographicOperations.FixedTimeEquals(presented.RawDataMemory.Span, expected.RawDataMemory.Span);
}

public interface ISentinelRequestAuthorizer
{
    SentinelProtocolError? Authorize(SentinelCapabilityRequest request);
}

public sealed class SentinelRequestAuthorizer : ISentinelRequestAuthorizer, IDisposable
{
    private static readonly TimeSpan MaximumFutureLifetime = TimeSpan.FromMinutes(1);
    private readonly SentinelProtocolOptions _options;
    private readonly ECDsa _verificationKey;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _replayCache = new(StringComparer.Ordinal);

    public SentinelRequestAuthorizer(IOptions<SentinelProtocolOptions> options)
    {
        _options = options.Value;
        _verificationKey = ECDsa.Create();
        _verificationKey.ImportFromPem(File.ReadAllText(_options.ProofPublicKeyPath));
    }

    public SentinelProtocolError? Authorize(SentinelCapabilityRequest request)
    {
        var now = DateTimeOffset.UtcNow;
        RemoveExpiredEntries(now);

        if (!string.Equals(request.NodeId, _options.NodeId, StringComparison.Ordinal))
        {
            return Denied("The authorization proof is not valid for this node.");
        }

        if (request.ExpiresAtUtc <= now || request.ExpiresAtUtc > now.Add(MaximumFutureLifetime))
        {
            return Denied("The authorization proof has expired or has an invalid lifetime.");
        }

        if (!string.Equals(
                request.AuthorizationProof.KeyId,
                _options.ProofKeyId,
                StringComparison.Ordinal))
        {
            return Denied("The authorization proof key is not trusted.");
        }

        byte[] signature;
        try
        {
            signature = Convert.FromBase64String(request.AuthorizationProof.Signature);
        }
        catch (FormatException)
        {
            return Denied("The authorization proof is malformed.");
        }

        var payload = SentinelRequestCanonicalizer.CreateSigningPayload(
            request.MessageId,
            request.NodeId,
            request.ExpiresAtUtc,
            request.Capability,
            request.Version,
            request.Arguments);

        if (!_verificationKey.VerifyData(payload, signature, HashAlgorithmName.SHA256))
        {
            return Denied("The authorization proof signature is invalid.");
        }

        if (!_replayCache.TryAdd(request.MessageId, request.ExpiresAtUtc))
        {
            return new SentinelProtocolError(
                "REPLAY_DETECTED",
                "The request has already been processed.",
                false);
        }

        return null;
    }

    public void Dispose() => _verificationKey.Dispose();

    private void RemoveExpiredEntries(DateTimeOffset now)
    {
        foreach (var entry in _replayCache)
        {
            if (entry.Value <= now)
            {
                _replayCache.TryRemove(entry.Key, out _);
            }
        }
    }

    private static SentinelProtocolError Denied(string message) =>
        new("LOCAL_POLICY_DENIED", message, false);
}
