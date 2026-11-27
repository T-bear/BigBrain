using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BigBrain.Finance.OutcomeAudit;
using Xunit;

namespace BigBrain.Finance.OutcomeAudit.Tests;

public sealed class SignedCommitmentReceiptVerifierTests
{
    private const string KeyId = "synthetic-key";
    private static readonly DateTimeOffset Now = new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);
    private static CommitmentClaims Claims => new("receipt", "issuer", "clock",
        "sha256:" + new string('a', 64), "sha256:" + new string('b', 64), Now.AddHours(-1), Now.AddHours(1));

    private static byte[] PublicKey(ECDsa key)
    {
        var point = key.ExportParameters(false).Q;
        return [4, .. point.X!, .. point.Y!];
    }

    private static byte[] Sign(ECDsa key, byte[] payload) =>
        [.. payload, .. key.SignData(payload, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation)];

    private static SignedCommitmentReceiptVerifier Verifier(ECDsa key) =>
        new(PublicKey(key), Claims.Issuer, KeyId, Claims.ClockSource);

    [Fact]
    public void CanonicalLayoutHasFixedDomainLengthsOrderHashesAndTickPrecision()
    {
        var claims = Claims with { WitnessedAt = Now.AddTicks(-1), ValidUntil = Now.AddTicks(1) };
        // Independent layout construction pins the wire contract, not just a sign/verify roundtrip.
        var expected = new List<byte>(Encoding.ASCII.GetBytes("BB073/receipt/v1/ECDSA-P256-SHA256/P1363\0"));
        foreach (var value in new[] { KeyId, "receipt", "issuer", "clock" })
        {
            expected.Add(0);
            expected.Add((byte)value.Length);
            expected.AddRange(Encoding.ASCII.GetBytes(value));
        }
        expected.AddRange(Enumerable.Repeat((byte)0xaa, 32));
        expected.AddRange(Enumerable.Repeat((byte)0xbb, 32));
        var ticks = new byte[16];
        BinaryPrimitives.WriteInt64BigEndian(ticks, Now.Ticks - 1);
        BinaryPrimitives.WriteInt64BigEndian(ticks.AsSpan(8), Now.Ticks + 1);
        expected.AddRange(ticks);
        Assert.Equal(expected.ToArray(), SignedCommitmentReceiptVerifier.CanonicalPayload(claims, KeyId));
    }

    [Theory]
    [InlineData("receipt")]
    [InlineData("issuer")]
    [InlineData("clock")]
    [InlineData("manifest")]
    [InlineData("bytes")]
    [InlineData("witness")]
    [InlineData("validity")]
    public void EveryClaimIsAuthenticated(string field)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var payload = SignedCommitmentReceiptVerifier.CanonicalPayload(Claims, KeyId);
        var receipt = Sign(key, payload);
        var changed = field switch
        {
            "receipt" => Claims with { ReceiptId = "other" },
            "issuer" => Claims with { Issuer = "other" },
            "clock" => Claims with { ClockSource = "other" },
            "manifest" => Claims with { ManifestHash = Claims.ManifestBytesHash },
            "bytes" => Claims with { ManifestBytesHash = Claims.ManifestHash },
            "witness" => Claims with { WitnessedAt = Claims.WitnessedAt.AddTicks(1) },
            _ => Claims with { ValidUntil = Claims.ValidUntil.AddTicks(1) }
        };
        Assert.True(Verifier(key).Verify(Claims, receipt, Now));
        Assert.False(Verifier(key).Verify(changed, receipt, Now));
        // Even an attacker rewriting the envelope to match the new claims cannot reuse the signature.
        byte[] rewritten = [.. SignedCommitmentReceiptVerifier.CanonicalPayload(changed, KeyId), .. receipt[^64..]];
        Assert.False(Verifier(key).Verify(changed, rewritten, Now));
    }

    [Theory]
    [InlineData("wrong-key")]
    [InlineData("key-id")]
    [InlineData("configured-issuer")]
    [InlineData("configured-clock")]
    [InlineData("signature")]
    [InlineData("zero-signature")]
    [InlineData("hash-as-signature")]
    [InlineData("der-signature")]
    [InlineData("truncated")]
    [InlineData("trailing")]
    [InlineData("empty")]
    [InlineData("oversized")]
    [InlineData("profile")]
    [InlineData("length-endianness")]
    [InlineData("field-order")]
    public void InvalidKeysSignaturesAndEvenSignedNoncanonicalPayloadsFail(string scenario)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var other = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var payload = SignedCommitmentReceiptVerifier.CanonicalPayload(Claims, KeyId);
        if (scenario == "profile") payload[0] ^= 1;
        if (scenario == "length-endianness")
        {
            var offset = Encoding.ASCII.GetByteCount("BB073/receipt/v1/ECDSA-P256-SHA256/P1363\0");
            (payload[offset], payload[offset + 1]) = (payload[offset + 1], payload[offset]);
        }
        if (scenario == "field-order") payload = SignedCommitmentReceiptVerifier.CanonicalPayload(
            Claims with { ReceiptId = Claims.Issuer, Issuer = Claims.ReceiptId }, KeyId);
        var receipt = Sign(key, payload);
        var verifier = scenario switch
        {
            "wrong-key" => Verifier(other),
            "key-id" => new(PublicKey(key), Claims.Issuer, "other-key-id", Claims.ClockSource),
            "configured-issuer" => new(PublicKey(key), "other", KeyId, Claims.ClockSource),
            "configured-clock" => new(PublicKey(key), Claims.Issuer, KeyId, "other"),
            _ => Verifier(key)
        };
        if (scenario == "signature") receipt[^1] ^= 1;
        if (scenario == "zero-signature") receipt = [.. payload, .. new byte[64]];
        if (scenario == "hash-as-signature") receipt = [.. payload, .. SHA256.HashData(payload), .. SHA256.HashData(payload)];
        if (scenario == "der-signature") receipt = [.. payload, .. key.SignData(payload, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)];
        if (scenario == "truncated") receipt = receipt[..^1];
        if (scenario == "trailing") receipt = [.. receipt, 0];
        if (scenario == "empty") receipt = [];
        if (scenario == "oversized") receipt = new byte[SignedCommitmentReceiptVerifier.MaxReceiptBytes + 1];
        Assert.False(verifier.Verify(Claims, receipt, Now));
    }

    [Theory]
    [InlineData("empty-id")]
    [InlineData("long-id")]
    [InlineData("unicode")]
    [InlineData("surrogate")]
    [InlineData("control")]
    [InlineData("space")]
    [InlineData("uppercase-hash")]
    [InlineData("bad-hash")]
    [InlineData("null-hash")]
    [InlineData("null-claims")]
    [InlineData("non-utc-witness")]
    [InlineData("non-utc-expiry")]
    [InlineData("default-time")]
    [InlineData("inverted-time")]
    public void InvalidClaimsFailClosed(string scenario)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var receipt = Sign(key, SignedCommitmentReceiptVerifier.CanonicalPayload(Claims, KeyId));
        var changed = scenario switch
        {
            "empty-id" => Claims with { ReceiptId = "" },
            "long-id" => Claims with { ReceiptId = new string('r', 257) },
            "unicode" => Claims with { ReceiptId = "récépissé" },
            "surrogate" => Claims with { ReceiptId = "\ud800" },
            "control" => Claims with { ReceiptId = "r\0" },
            "space" => Claims with { ReceiptId = "r " },
            "uppercase-hash" => Claims with { ManifestHash = Claims.ManifestHash.ToUpperInvariant() },
            "bad-hash" => Claims with { ManifestBytesHash = "sha256:bad" },
            "null-hash" => Claims with { ManifestHash = null! },
            "null-claims" => null!,
            "non-utc-witness" => Claims with { WitnessedAt = Claims.WitnessedAt.ToOffset(TimeSpan.FromHours(1)) },
            "non-utc-expiry" => Claims with { ValidUntil = Claims.ValidUntil.ToOffset(TimeSpan.FromHours(1)) },
            "default-time" => Claims with { WitnessedAt = default },
            _ => Claims with { ValidUntil = Claims.WitnessedAt.AddTicks(-1) }
        };
        Assert.False(Verifier(key).Verify(changed, receipt, Now));
        Assert.Throws<ArgumentException>(() => SignedCommitmentReceiptVerifier.CanonicalPayload(changed, KeyId));
    }

    [Fact]
    public void SuppliedTimeIsCheckedExactlyButDoesNotAttestClockTrust()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var receipt = Sign(key, SignedCommitmentReceiptVerifier.CanonicalPayload(Claims, KeyId));
        var verifier = Verifier(key);
        Assert.True(verifier.Verify(Claims, receipt, Claims.WitnessedAt));
        Assert.True(verifier.Verify(Claims, receipt, Claims.ValidUntil));
        Assert.False(verifier.Verify(Claims, receipt, Claims.WitnessedAt.AddTicks(-1)));
        Assert.False(verifier.Verify(Claims, receipt, Claims.ValidUntil.AddTicks(1)));
        Assert.False(verifier.Verify(Claims, receipt, Now.ToOffset(TimeSpan.FromHours(1))));
        Assert.False(verifier.Verify(Claims, receipt, default));
    }

    [Fact]
    public void ConfigurationRequiresExactPublicPointAndIdentitiesAndCopiesTheKey()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var wrongCurve = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        Assert.Throws<ArgumentException>(() => new SignedCommitmentReceiptVerifier([], "i", "k", "c"));
        Assert.Throws<ArgumentException>(() => new SignedCommitmentReceiptVerifier(PublicKey(wrongCurve), "i", "k", "c"));
        foreach (var invalid in new[] { "", " ", "é", new string('k', 257), null! })
        {
            Assert.Throws<ArgumentException>(() => new SignedCommitmentReceiptVerifier(PublicKey(key), invalid, "k", "c"));
            Assert.Throws<ArgumentException>(() => new SignedCommitmentReceiptVerifier(PublicKey(key), "i", invalid, "c"));
            Assert.Throws<ArgumentException>(() => new SignedCommitmentReceiptVerifier(PublicKey(key), "i", "k", invalid));
        }
        byte[] invalidPoint = [4, .. new byte[64]];
        Assert.ThrowsAny<CryptographicException>(() => new SignedCommitmentReceiptVerifier(invalidPoint, "i", "k", "c"));
        var bytes = PublicKey(key);
        var verifier = new SignedCommitmentReceiptVerifier(bytes, Claims.Issuer, KeyId, Claims.ClockSource);
        Array.Clear(bytes);
        Assert.True(verifier.Verify(Claims, Sign(key, SignedCommitmentReceiptVerifier.CanonicalPayload(Claims, KeyId)), Now));
    }

    [Theory]
    [InlineData("positive")]
    [InlineData("duplicate")]
    [InlineData("wrong-identity")]
    [InlineData("default-verifier")]
    public void VerifiedSignatureNeverAdmitsRealEvidenceAndDuplicateReplayFails(string scenario)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var (manifest, rows) = ForwardEvaluationTests.Sequence();
        var asOf = rows[^1].EvaluatedAt.AddDays(1);
        var claims = Claims with
        {
            ManifestHash = ForwardEvaluation.ManifestHash(manifest),
            ManifestBytesHash = FrozenCommitment.Digest(JsonSerializer.SerializeToUtf8Bytes(manifest)),
            WitnessedAt = manifest.FrozenAt.AddHours(1),
            ValidUntil = asOf.AddDays(1)
        };
        var receipt = Sign(key, SignedCommitmentReceiptVerifier.CanonicalPayload(claims, KeyId));
        var verifier = Verifier(key);
        Assert.True(verifier.Verify(claims, receipt, asOf));
        // Pure verification is repeatable; it cannot provide durable replay prevention.
        Assert.True(verifier.Verify(claims, receipt, asOf));
        string[] identities = scenario switch
        {
            "duplicate" => [claims.ReceiptId, claims.ReceiptId],
            "wrong-identity" => ["other"],
            _ => [claims.ReceiptId]
        };
        var report = ProspectiveCommitment.EvaluateSynthetic(new(manifest, claims, receipt), rows,
            ForwardEvaluation.EvidenceHash(rows), asOf, identities, scenario == "default-verifier" ? null : verifier);
        Assert.Equal("NOT_VERIFIED", report.IndependentAttestedBoundary);
        Assert.False(report.RealProspectiveAdmission);
        Assert.Equal("SYNTHETIC_ONLY", report.Evaluation.EvidenceKind);
        Assert.Equal(scenario == "positive" ? "SYNTHETIC_FIXTURE_PROVEN" : "NOT_PROVEN", report.SyntheticBoundary);
        Assert.Equal(scenario == "positive" ? "EVALUABLE" : "NOT_EVALUABLE", report.Evaluation.Status);
        if (scenario != "positive")
        {
            Assert.Equal(0, report.Evaluation.GenuineFutureSessions);
            Assert.Equal(ForwardMetrics.Unknown, report.Evaluation.Metrics);
            Assert.Empty(report.Evaluation.Groups);
        }
    }
}
