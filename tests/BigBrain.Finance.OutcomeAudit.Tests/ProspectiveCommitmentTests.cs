using System.Text.Json;
using BigBrain.Finance.OutcomeAudit;
using Xunit;

namespace BigBrain.Finance.OutcomeAudit.Tests;

public sealed class ProspectiveCommitmentTests
{
    private sealed class FixtureVerifier(CommitmentClaims expected, byte[] bytes) : ICommitmentReceiptVerifier
    {
        public bool Verify(CommitmentClaims claims, ReadOnlySpan<byte> receiptBytes, DateTimeOffset asOf) =>
            claims == expected && receiptBytes.SequenceEqual(bytes) && asOf <= expected.ValidUntil;
    }

    [Theory]
    [InlineData("positive")]
    [InlineData("default-verifier")]
    [InlineData("forged")]
    [InlineData("retroactive")]
    [InlineData("tampered")]
    [InlineData("empty-receipt")]
    [InlineData("before-freeze")]
    [InlineData("missing-clock")]
    [InlineData("stale")]
    [InlineData("wrong-manifest")]
    [InlineData("wrong-bytes-hash")]
    [InlineData("missing")]
    [InlineData("future-commit")]
    [InlineData("duplicate-receipt")]
    [InlineData("wrong-receipt-id")]
    [InlineData("wrong-clock")]
    [InlineData("wrong-issuer")]
    [InlineData("incomplete-cohort")]
    [InlineData("altered-key")]
    [InlineData("retrospective-row")]
    [InlineData("late-feature")]
    [InlineData("evidence-tampered")]
    public void OnlyCompleteIsolatedFixtureCanProveSyntheticBoundary(string scenario)
    {
        var (manifest, rows) = ForwardEvaluationTests.Sequence();
        var asOf = rows[^1].EvaluatedAt.AddDays(1);
        byte[] receipt = [1, 2, 3, 4]; // Synthetic bytes, never a signature.
        var original = new CommitmentClaims("fixture-receipt", "fixture-witness", "fixture-clock",
            ForwardEvaluation.ManifestHash(manifest), FrozenCommitment.Digest(JsonSerializer.SerializeToUtf8Bytes(manifest)),
            manifest.FrozenAt.AddHours(1), asOf.AddDays(1));
        var claims = scenario switch
        {
            "before-freeze" => original with { WitnessedAt = manifest.FrozenAt.AddTicks(-1) },
            "missing-clock" => original with { ClockSource = "" },
            "forged" => original with { WitnessedAt = original.WitnessedAt.AddMinutes(1) },
            "retroactive" => original with { WitnessedAt = rows[0].Cutoff.AddTicks(1) },
            "stale" => original with { ValidUntil = asOf.AddTicks(-1) },
            "wrong-manifest" => original with { ManifestHash = "sha256:wrong" },
            "wrong-bytes-hash" => original with { ManifestBytesHash = "sha256:wrong" },
            "future-commit" => original with { WitnessedAt = asOf.AddHours(1) },
            "wrong-clock" => original with { ClockSource = "another-clock" },
            "wrong-issuer" => original with { Issuer = "another-issuer" },
            _ => original
        };
        // For chronology defects the synthetic verifier agrees to the bad claims:
        // logical gates must independently reject them.
        var verifierClaims = scenario is "retroactive" or "stale" or "future-commit" or "before-freeze" ? claims : original;
        var verifier = new FixtureVerifier(verifierClaims, receipt.ToArray());
        if (scenario == "tampered") receipt[0] = 99;
        if (scenario == "empty-receipt") receipt = [];
        var commitment = new FrozenCommitment(manifest, claims, receipt);
        if (scenario == "incomplete-cohort") rows = rows[..119];
        if (scenario == "altered-key") rows[0] = rows[0] with { PredictionKey = rows[0].PredictionKey with { StrategyVersion = "v2" } };
        if (scenario == "retrospective-row") rows[0] = rows[0] with { Retrospective = true };
        if (scenario == "late-feature") rows[0] = rows[0] with { FeatureKnownAt = rows[0].Cutoff.AddTicks(1) };
        var evidenceHash = ForwardEvaluation.EvidenceHash(rows);
        if (scenario == "evidence-tampered") rows[0] = rows[0] with { RealizedClose = 999m };
        string[] identities = scenario switch
        {
            "duplicate-receipt" => [claims.ReceiptId, claims.ReceiptId],
            "wrong-receipt-id" => ["another-receipt"],
            _ => [claims.ReceiptId]
        };
        var report = ProspectiveCommitment.EvaluateSynthetic(scenario == "missing" ? null : commitment,
            rows, evidenceHash, asOf, identities, scenario == "default-verifier" ? null : verifier);
        Assert.Equal("NOT_VERIFIED", report.IndependentAttestedBoundary);
        Assert.False(report.RealProspectiveAdmission);
        Assert.Equal("SYNTHETIC_ONLY", report.Evaluation.EvidenceKind);
        if (scenario == "positive")
        {
            Assert.Equal("SYNTHETIC_FIXTURE_PROVEN", report.SyntheticBoundary);
            Assert.Equal("EVALUABLE", report.Evaluation.Status);
            Assert.Equal(120, report.Evaluation.GenuineFutureSessions);
        }
        else
        {
            Assert.Equal("NOT_PROVEN", report.SyntheticBoundary);
            Assert.Equal("NOT_EVALUABLE", report.Evaluation.Status);
            Assert.Equal(0, report.Evaluation.GenuineFutureSessions);
            Assert.Equal(ForwardMetrics.Unknown, report.Evaluation.Metrics);
            Assert.Empty(report.Evaluation.Groups);
        }
    }

    [Fact]
    public void SnapshotDoesNotExposeMutableManifestOrReceiptBytes()
    {
        var (manifest, rows) = ForwardEvaluationTests.Sequence();
        var hash = ForwardEvaluation.ManifestHash(manifest);
        byte[] receipt = [1, 2, 3];
        var claims = new CommitmentClaims("r", "i", "c", hash,
            FrozenCommitment.Digest(JsonSerializer.SerializeToUtf8Bytes(manifest)), manifest.FrozenAt, rows[^1].EvaluatedAt);
        var frozen = new FrozenCommitment(manifest, claims, receipt);
        var verifier = new FixtureVerifier(claims, receipt.ToArray());
        receipt[0] = 99;
        manifest.TargetSessions[0] = default;
        frozen.Manifest().Series[0] = manifest.Series[0] with { StrategyVersion = "changed" };
        Assert.Equal(hash, ForwardEvaluation.ManifestHash(frozen.Manifest()));
        Assert.True(frozen.Verify(verifier, rows[^1].EvaluatedAt));
    }
}
