namespace BigBrain.Sentinel.Tests;

public sealed class SentinelProtocolOptionsValidatorTests
{
    [Fact]
    public void ValidatorAcceptsDistinctExistingAllowlistedFilesystems()
    {
        var directory = CreateDirectory();
        try
        {
            var result = new SentinelProtocolOptionsValidator().Validate(
                null,
                CreateOptions(
                    new SentinelFilesystemOptions
                    {
                        FilesystemId = "system",
                        DisplayName = "System Storage",
                        SentinelPath = directory
                    }));

            Assert.True(result.Succeeded);
        }
        finally
        {
            Directory.Delete(directory);
        }
    }

    [Fact]
    public void ValidatorRejectsInvalidFilesystemAllowlist()
    {
        var result = new SentinelProtocolOptionsValidator().Validate(
            null,
            CreateOptions(
                new SentinelFilesystemOptions
                {
                    FilesystemId = "duplicate",
                    DisplayName = "System Storage",
                    SentinelPath = "/missing/system"
                },
                new SentinelFilesystemOptions
                {
                    FilesystemId = "duplicate",
                    DisplayName = "",
                    SentinelPath = "relative"
                },
                new SentinelFilesystemOptions
                {
                    FilesystemId = "",
                    DisplayName = "Missing ID",
                    SentinelPath = "/missing/id"
                }));

        Assert.False(result.Succeeded);
        var failures = Assert.IsAssignableFrom<IEnumerable<string>>(result.Failures);
        Assert.Contains(failures, failure => failure.Contains("must be unique", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Contains("DisplayName is required", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Contains("absolute path", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Contains("must exist", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Contains("FilesystemId is required", StringComparison.Ordinal));
    }

    private static SentinelProtocolOptions CreateOptions(
        params SentinelFilesystemOptions[] filesystems) =>
        new()
        {
            Enabled = true,
            NodeId = "node:test",
            SocketPath = "/run/bigbrain/sentinel.sock",
            ServerCertificatePath = "/identity/server.p12",
            TrustedClientCertificatePath = "/identity/client.p12",
            ProofPublicKeyPath = "/identity/proof.pem",
            ProofKeyId = "test",
            Filesystems = filesystems
        };

    private static string CreateDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"bigbrain-filesystem-options-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }
}
