using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using BigBrain.Api.Sentinel;
using BigBrain.Sentinel.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ApiSentinelClientOptions = BigBrain.Api.Sentinel.SentinelClientOptions;
using SentinelProtocolOptions = BigBrain.Sentinel.SentinelProtocolOptions;

namespace BigBrain.Sentinel.Tests;

public sealed class SentinelProtocolIntegrationTests
{
    [Fact]
    public async Task ControlPlanePingsSentinelOverMutuallyAuthenticatedUnixSocket()
    {
        await using var environment = await SentinelProtocolTestEnvironment.StartAsync();

        var ping = await environment.Client.PingAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Healthy", ping.Status);
        Assert.Equal(environment.NodeId, ping.NodeId);
        Assert.Equal(5, ping.CapabilityCount);
        Assert.True(ping.CheckedAtUtc <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task SystemMetricsRequestReturnsAllConfiguredHostMetrics()
    {
        await using var environment = await SentinelProtocolTestEnvironment.StartAsync();

        var snapshot = await environment.Client.ReadSystemMetricsAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal("available", snapshot.Status);
        Assert.Equal(310_920, snapshot.Sections.Uptime.Data?.UptimeSeconds);
        Assert.Equal("available", snapshot.Sections.Uptime.Status);
        Assert.Equal("available", snapshot.Sections.Cpu.Status);
        Assert.InRange(snapshot.Sections.Cpu.Data!.UsagePercent, 0, 100);
        Assert.True(snapshot.Sections.Cpu.Data.LogicalProcessorCount > 0);
        Assert.True(snapshot.Sections.Cpu.Data.SampleWindowMilliseconds > 0);
        Assert.Equal("available", snapshot.Sections.Memory.Status);
        Assert.True(snapshot.Sections.Memory.Data!.TotalBytes > 0);
        Assert.InRange(
            snapshot.Sections.Memory.Data.AvailableBytes,
            0,
            snapshot.Sections.Memory.Data.TotalBytes);
        Assert.Equal(
            snapshot.Sections.Memory.Data.TotalBytes
                - snapshot.Sections.Memory.Data.AvailableBytes,
            snapshot.Sections.Memory.Data.UsedBytes);
        Assert.InRange(snapshot.Sections.Memory.Data.UsagePercent, 0, 100);
        Assert.Equal("available", snapshot.Sections.Disks.Status);
        var disk = Assert.Single(snapshot.Sections.Disks.Items);
        Assert.Equal("integration", disk.FilesystemId);
        Assert.Equal("Integration Storage", disk.DisplayName);
        Assert.Equal("available", disk.Status);
        Assert.True(disk.TotalBytes > 0);
        Assert.Equal(disk.TotalBytes - disk.AvailableBytes, disk.UsedBytes);
        Assert.InRange(disk.UsagePercent!.Value, 0, 100);
        Assert.Empty(snapshot.Warnings);
    }

    [Fact]
    public async Task UntrustedControlPlaneCertificateCannotPingSentinel()
    {
        await using var environment = await SentinelProtocolTestEnvironment.StartAsync(
            useUntrustedClientCertificate: true);

        var exception = await Assert.ThrowsAsync<SentinelClientUnavailableException>(
            () => environment.Client.PingAsync(TestContext.Current.CancellationToken));

        Assert.Contains("Ping could not be completed", exception.Message, StringComparison.Ordinal);
    }

    private sealed class SentinelProtocolTestEnvironment : IAsyncDisposable
    {
        private readonly string _directory;
        private readonly WebApplication _application;

        private SentinelProtocolTestEnvironment(
            string directory,
            WebApplication application,
            LocalSentinelClient client,
            string nodeId)
        {
            _directory = directory;
            _application = application;
            Client = client;
            NodeId = nodeId;
        }

        public LocalSentinelClient Client { get; }

        public string NodeId { get; }

        public static async Task<SentinelProtocolTestEnvironment> StartAsync(
            bool useUntrustedClientCertificate = false)
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                $"bigbrain-sentinel-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);

            try
            {
                var nodeId = "node:integration-test";
                var socketPath = Path.Combine(directory, "sentinel.sock");
                await File.WriteAllTextAsync(socketPath, "stale-socket", TestContext.Current.CancellationToken);
                var serverCertificatePath = Path.Combine(directory, "server.pfx");
                var clientCertificatePath = Path.Combine(directory, "client.pfx");
                var untrustedClientCertificatePath = Path.Combine(directory, "untrusted-client.pfx");
                var proofPrivateKeyPath = Path.Combine(directory, "proof-private.pem");
                var proofPublicKeyPath = Path.Combine(directory, "proof-public.pem");

                WriteCertificate(serverCertificatePath, "bigbrain-sentinel");
                WriteCertificate(clientCertificatePath, "bigbrain-control-plane");
                WriteCertificate(untrustedClientCertificatePath, "untrusted-control-plane");
                WriteProofKeys(proofPrivateKeyPath, proofPublicKeyPath);

                var builder = SentinelHost.CreateBuilder(
                [
                    $"--{SentinelProtocolOptions.SectionName}:Enabled=true",
                    $"--{SentinelProtocolOptions.SectionName}:NodeId={nodeId}",
                    $"--{SentinelProtocolOptions.SectionName}:SocketPath={socketPath}",
                    $"--{SentinelProtocolOptions.SectionName}:ServerCertificatePath={serverCertificatePath}",
                    $"--{SentinelProtocolOptions.SectionName}:TrustedClientCertificatePath={clientCertificatePath}",
                    $"--{SentinelProtocolOptions.SectionName}:ProofPublicKeyPath={proofPublicKeyPath}",
                    $"--{SentinelProtocolOptions.SectionName}:ProofKeyId=integration-test",
                    $"--{SentinelProtocolOptions.SectionName}:Filesystems:0:FilesystemId=integration",
                    $"--{SentinelProtocolOptions.SectionName}:Filesystems:0:DisplayName=Integration Storage",
                    $"--{SentinelProtocolOptions.SectionName}:Filesystems:0:SentinelPath={directory}"
                ]);
                builder.Services.RemoveAll<IHostUptimeReader>();
                builder.Services.AddSingleton<IHostUptimeReader>(new FixedHostUptimeReader(310_920));
                var application = SentinelHost.Build(builder);
                await application.StartAsync(TestContext.Current.CancellationToken);

                var clientOptions = Options.Create(
                    new ApiSentinelClientOptions
                    {
                        Enabled = true,
                        NodeId = nodeId,
                        SocketPath = socketPath,
                        ClientCertificatePath = useUntrustedClientCertificate
                            ? untrustedClientCertificatePath
                            : clientCertificatePath,
                        TrustedServerCertificatePath = serverCertificatePath,
                        ProofPrivateKeyPath = proofPrivateKeyPath,
                        ProofKeyId = "integration-test"
                    });
                var client = new LocalSentinelClient(clientOptions);

                return new SentinelProtocolTestEnvironment(
                    directory,
                    application,
                    client,
                    nodeId);
            }
            catch
            {
                Directory.Delete(directory, recursive: true);
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _application.StopAsync();
            await _application.DisposeAsync();
            Directory.Delete(_directory, recursive: true);
        }

        private static void WriteCertificate(string path, string commonName)
        {
            using var key = RSA.Create(2048);
            var request = new CertificateRequest(
                $"CN={commonName}",
                key,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(
                    certificateAuthority: false,
                    hasPathLengthConstraint: false,
                    pathLengthConstraint: 0,
                    critical: true));
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature,
                    critical: true));
            var subjectAlternativeName = new SubjectAlternativeNameBuilder();
            subjectAlternativeName.AddDnsName(commonName);
            request.CertificateExtensions.Add(subjectAlternativeName.Build());

            using var certificate = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddMinutes(-1),
                DateTimeOffset.UtcNow.AddHours(1));
            File.WriteAllBytes(path, certificate.Export(X509ContentType.Pkcs12));
        }

        private static void WriteProofKeys(string privatePath, string publicPath)
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            File.WriteAllText(privatePath, key.ExportPkcs8PrivateKeyPem());
            File.WriteAllText(publicPath, key.ExportSubjectPublicKeyInfoPem());
        }

        private sealed class FixedHostUptimeReader(double uptimeSeconds) : IHostUptimeReader
        {
            public double ReadUptimeSeconds() => uptimeSeconds;
        }
    }
}
