using System.Security.Cryptography;
using System.Text.Json;

namespace BigBrain.Finance.OutcomeAudit;

// Claims are NOT authentication. An independent receipt/issuer/clock verifier is an
// external precondition. Offline signature verification grants no admission authority.
internal sealed record CommitmentClaims(string ReceiptId, string Issuer, string ClockSource,
    string ManifestHash, string ManifestBytesHash, DateTimeOffset WitnessedAt, DateTimeOffset ValidUntil);

internal interface ICommitmentReceiptVerifier
{
    // Verify exact bytes, all claims, configured identity and validity at supplied asOf.
    // A true result alone does not establish independent key trust or clock provenance.
    bool Verify(CommitmentClaims claims, ReadOnlySpan<byte> receiptBytes, DateTimeOffset asOf);
}

internal sealed class RejectCommitmentReceiptVerifier : ICommitmentReceiptVerifier
{
    public bool Verify(CommitmentClaims claims, ReadOnlySpan<byte> receiptBytes, DateTimeOffset asOf) => false;
}

internal sealed class FrozenCommitment
{
    private readonly byte[] manifestBytes;
    private readonly byte[] receiptBytes;
    internal CommitmentClaims Claims { get; }
    internal string ManifestBytesHash { get; }
    internal string ReceiptBytesHash { get; }

    internal FrozenCommitment(ForwardManifest manifest, CommitmentClaims claims, ReadOnlySpan<byte> receipt)
    {
        manifestBytes = JsonSerializer.SerializeToUtf8Bytes(manifest);
        receiptBytes = receipt.ToArray();
        Claims = claims;
        ManifestBytesHash = Digest(manifestBytes);
        ReceiptBytesHash = Digest(receiptBytes);
    }

    internal ForwardManifest Manifest() => JsonSerializer.Deserialize<ForwardManifest>(manifestBytes)!;
    internal bool Verify(ICommitmentReceiptVerifier verifier, DateTimeOffset asOf) =>
        receiptBytes.Length > 0 && ManifestBytesHash == Digest(manifestBytes) &&
        ReceiptBytesHash == Digest(receiptBytes) && verifier.Verify(Claims, receiptBytes, asOf);
    internal static string Digest(ReadOnlySpan<byte> bytes) => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));
}

internal sealed record CommitmentReport(string IndependentAttestedBoundary, string SyntheticBoundary,
    bool RealProspectiveAdmission, ForwardReport Evaluation);

internal static class ProspectiveCommitment
{
    // No production call site. Even a positive injected test verifier proves only a fixture.
    internal static CommitmentReport EvaluateSynthetic(FrozenCommitment? commitment,
        IReadOnlyList<ForwardEvidence> evidence, string expectedEvidenceHash, DateTimeOffset asOf,
        IReadOnlyList<string> receiptIdentities, ICommitmentReceiptVerifier? verifier = null)
    {
        CommitmentReport Stop(string reason) => new("NOT_VERIFIED", "NOT_PROVEN", false,
            new(ForwardEvaluation.Contract, "SYNTHETIC_ONLY", "NOT_EVALUABLE", reason,
                commitment?.Claims?.ManifestHash ?? "", expectedEvidenceHash, 0,
                ForwardMetrics.Unknown, Array.Empty<ForwardGroup>()));

        if (commitment is null) return Stop("missing-receipt");
        var claims = commitment.Claims;
        if (claims is null || !Text(claims.ReceiptId) || !Text(claims.Issuer) || !Text(claims.ClockSource) ||
            receiptIdentities is null || receiptIdentities.Count != 1 || receiptIdentities[0] != claims.ReceiptId)
            return Stop("invalid-or-duplicate-receipt-identity");
        var manifest = commitment.Manifest();
        if (manifest is null || claims.ManifestHash != ForwardEvaluation.ManifestHash(manifest) ||
            claims.ManifestBytesHash != commitment.ManifestBytesHash)
            return Stop("wrong-manifest");
        if (!Utc(asOf) || !Utc(claims.WitnessedAt) || !Utc(claims.ValidUntil) ||
            claims.WitnessedAt < manifest.FrozenAt || claims.WitnessedAt > asOf ||
            claims.ValidUntil < claims.WitnessedAt || asOf > claims.ValidUntil)
            return Stop("invalid-or-stale-commitment-time");
        // Validate logical chronology before any acceptance; the evaluator additionally
        // enforces named consecutive sessions, full panel, exact keys and all knowledge times.
        if (evidence is null || evidence.Any(row => row is null || row.Retrospective ||
            row.Cutoff < claims.WitnessedAt || row.CreatedAt < claims.WitnessedAt))
            return Stop("retroactive-commitment");
        try
        {
            if (!commitment.Verify(verifier ?? new RejectCommitmentReceiptVerifier(), asOf))
                return Stop("receipt-not-verified");
        }
        catch (Exception)
        {
            // A verifier failure cannot turn into partial acceptance or expose its payload.
            return Stop("receipt-not-verified");
        }
        var result = ForwardEvaluation.Evaluate(manifest, claims.ManifestHash, evidence, expectedEvidenceHash, asOf);
        return new("NOT_VERIFIED", result.Status == "EVALUABLE" ? "SYNTHETIC_FIXTURE_PROVEN" : "NOT_PROVEN", false, result);
    }

    private static bool Utc(DateTimeOffset value) => value != default && value.Offset == TimeSpan.Zero;
    private static bool Text(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 256 && !value.Any(char.IsControl);
}
