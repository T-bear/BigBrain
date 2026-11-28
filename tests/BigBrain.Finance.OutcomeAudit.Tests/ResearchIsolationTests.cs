using System.Collections;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using BigBrain.Finance.OutcomeAudit;
using Microsoft.Data.Sqlite;
using Xunit;

namespace BigBrain.Finance.OutcomeAudit.Tests;

// Only the dedicated runner supplies this environment. Ordinary backend runs SKIP
// these tests; the isolated CI invocation treats every skip as failure.
public sealed class ResearchIsolationTests
{
    public static bool IsIsolated => Environment.GetEnvironmentVariable("BB_RESEARCH_ISOLATED") == "1";

    [Fact(Skip = "Requires the isolated synthetic runner", SkipUnless = nameof(IsIsolated))]
    public void EnvironmentAndFilesystemExposeNoHostCredentialsOrProductionMounts()
    {
        string[] allowed = ["BB_RESEARCH_ISOLATED", "HOME", "TMPDIR", "DOTNET_ROOT",
            "DOTNET_EnableDiagnostics", "DOTNET_CLI_TELEMETRY_OPTOUT", "LANG", "PWD"];
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
            Assert.Contains((string)entry.Key, allowed);
        Assert.Equal("/app", Environment.GetEnvironmentVariable("PWD"));
        Assert.Equal("# Empty synthetic DNS configuration; no nameservers.\n", File.ReadAllText("/etc/resolv.conf"));
        Assert.Empty(Directory.GetFileSystemEntries("/home"));
        Assert.False(Directory.Exists("/run"));
        Assert.False(Directory.Exists("/var"));
        Assert.False(Directory.Exists("/root"));
        Assert.False(File.Exists("/app/.env"));
        Assert.False(File.Exists("/source/.env"));
        Assert.Equal("synthetic-only", File.ReadAllText("/source/identity"));
    }

    [Fact(Skip = "Requires the isolated synthetic runner", SkipUnless = nameof(IsIsolated))]
    public async Task SyntheticOrderTransportHasNoNetworkOrLocalBrokerSocket()
    {
        Assert.Equal(NetworkInterfaceType.Loopback,
            Assert.Single(NetworkInterface.GetAllNetworkInterfaces()).NetworkInterfaceType);
        using var internet = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        // TEST-NET address inside an already isolated namespace; never a provider.
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await Assert.ThrowsAsync<SocketException>(async () =>
            await internet.ConnectAsync(new IPEndPoint(IPAddress.Parse("192.0.2.1"), 443), timeout.Token));
        using var local = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        Assert.Throws<SocketException>(() => local.Connect(new UnixDomainSocketEndPoint("/source/orders.sock")));
        // A successful connection makes the test red before any payload can be sent.
    }

    [Fact(Skip = "Requires the isolated synthetic runner", SkipUnless = nameof(IsIsolated))]
    public void SyntheticProductionSurrogateRejectsSqlAndFilesystemWrites()
    {
        const string path = "/source/production-surrogate.db";
        var before = File.ReadAllBytes(path);
        using (var connection = OfflineImage.OpenImmutable(path))
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA query_only=OFF; DELETE FROM sentinel";
            Assert.Equal(8, Assert.Throws<SqliteException>(() => command.ExecuteNonQuery()).SqliteErrorCode);
        }
        // Test the mount independently of the library's connection mode.
        Assert.ThrowsAny<IOException>(() => File.WriteAllText(path, "forbidden"));
        Assert.ThrowsAny<IOException>(() => File.WriteAllText("/source/new-order", "forbidden"));
        Assert.ThrowsAny<IOException>(() => File.Delete(path));
        Assert.Equal(before, File.ReadAllBytes(path));
        Assert.False(File.Exists(path + "-wal"));
        Assert.False(File.Exists(path + "-shm"));
    }

    [Fact]
    public void BothCompiledResearchLibrariesRejectUnreviewedClientDependencies()
    {
        foreach (var assembly in new[] { typeof(OfflineImage).Assembly, typeof(JournalProvenance).Assembly })
        {
            Assert.Null(assembly.EntryPoint);
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                var name = reference.Name!;
                Assert.True(name.StartsWith("System.", StringComparison.Ordinal) ||
                    name is "Microsoft.Data.Sqlite" or "BigBrain.Finance.OutcomeAudit",
                    "Unreviewed research assembly dependency");
                Assert.False(name.StartsWith("System.Net", StringComparison.Ordinal));
                Assert.False(name.StartsWith("System.Reflection", StringComparison.Ordinal));
                Assert.False(name.StartsWith("System.Diagnostics.Process", StringComparison.Ordinal));
            }
        }
    }
}
