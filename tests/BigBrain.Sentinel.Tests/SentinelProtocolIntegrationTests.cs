using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using BigBrain.Api.Sentinel;
using BigBrain.Sentinel.Contracts;
using Microsoft.AspNetCore.Builder;
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
        Assert.Equal(1, ping.CapabilityCount);
        Assert.True(ping.CheckedAtUtc <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task SystemMetricsRequestIsAuthenticatedAndReturnsNotImplemented()
    {
        await using var environment = await SentinelProtocolTestEnvironment.StartAsync();

        var error = await environment.Client.ReadSystemMetricsAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(SentinelProtocol.CapabilityUnavailable, error.Code);
        Assert.Equal("System Metrics collection is not implemented.", error.Message);
        Assert.False(error.Retryable);
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
                    $"--{SentinelProtocolOptions.SectionName}:ProofKeyId=integration-test"
                ]);
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
    }
}
