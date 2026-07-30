using System.Net;
using System.Net.Http.Json;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using BigBrain.Sentinel.Contracts;
using Microsoft.Extensions.Options;

namespace BigBrain.Api.Sentinel;

public interface ISentinelClient
{
    Task<SentinelPingResponse> PingAsync(CancellationToken cancellationToken);

    Task<SentinelProtocolError> ReadSystemMetricsAsync(CancellationToken cancellationToken);
}

public sealed class SentinelClientUnavailableException : Exception
{
    public SentinelClientUnavailableException(string message)
        : base(message)
    {
    }

    public SentinelClientUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class DisabledSentinelClient : ISentinelClient
{
    public Task<SentinelPingResponse> PingAsync(CancellationToken cancellationToken) =>
        Task.FromException<SentinelPingResponse>(
            new SentinelClientUnavailableException("Sentinel communication is not configured."));

    public Task<SentinelProtocolError> ReadSystemMetricsAsync(CancellationToken cancellationToken) =>
        Task.FromException<SentinelProtocolError>(
            new SentinelClientUnavailableException("Sentinel communication is not configured."));
}

public sealed class LocalSentinelClient : ISentinelClient, IDisposable
{
    private static readonly TimeSpan RequestLifetime = TimeSpan.FromSeconds(30);
    private readonly SentinelClientOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ECDsa _proofKey;

    public LocalSentinelClient(IOptions<SentinelClientOptions> options)
    {
        _options = options.Value;
        _httpClient = CreateHttpClient(_options);
        _proofKey = ECDsa.Create();
        _proofKey.ImportFromPem(File.ReadAllText(_options.ProofPrivateKeyPath));
    }

    public async Task<SentinelPingResponse> PingAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<SentinelPingResponse>(
                SentinelProtocol.PingPath,
                cancellationToken);
            if (response is null
                || !string.Equals(response.NodeId, _options.NodeId, StringComparison.Ordinal))
            {
                throw new SentinelClientUnavailableException("Sentinel returned an invalid Ping response.");
            }

            return response;
        }
        catch (SentinelClientUnavailableException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is HttpRequestException
            or IOException
            or TaskCanceledException)
        {
            throw new SentinelClientUnavailableException(
                "Sentinel Ping could not be completed.",
                exception);
        }
    }

    public async Task<SentinelProtocolError> ReadSystemMetricsAsync(
        CancellationToken cancellationToken)
    {
        var arguments = SentinelSnapshotRequest.CreateArguments();
        var messageId = $"message:{Guid.NewGuid():N}";
        var expiresAtUtc = DateTimeOffset.UtcNow.Add(RequestLifetime);
        var payload = SentinelRequestCanonicalizer.CreateSigningPayload(
            messageId,
            _options.NodeId,
            expiresAtUtc,
            SentinelProtocol.InventoryReadSnapshot,
            SentinelProtocol.InventoryReadSnapshotVersion,
            arguments);
        var signature = _proofKey.SignData(payload, HashAlgorithmName.SHA256);
        var request = new SentinelCapabilityRequest(
            messageId,
            _options.NodeId,
            expiresAtUtc,
            SentinelProtocol.InventoryReadSnapshot,
            SentinelProtocol.InventoryReadSnapshotVersion,
            arguments,
            new SentinelAuthorizationProof(_options.ProofKeyId, Convert.ToBase64String(signature)));

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                SentinelProtocol.ReadSnapshotPath,
                request,
                cancellationToken);
            var error = await response.Content.ReadFromJsonAsync<SentinelProtocolError>(
                cancellationToken);

            if (response.StatusCode != HttpStatusCode.NotImplemented
                || error?.Code != SentinelProtocol.CapabilityUnavailable)
            {
                throw new SentinelClientUnavailableException(
                    "Sentinel returned an unexpected System Metrics response.");
            }

            return error;
        }
        catch (SentinelClientUnavailableException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is HttpRequestException
            or IOException
            or TaskCanceledException)
        {
            throw new SentinelClientUnavailableException(
                "Sentinel System Metrics request could not be completed.",
                exception);
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _proofKey.Dispose();
    }

    private static HttpClient CreateHttpClient(SentinelClientOptions options)
    {
        var clientCertificate = LoadCertificate(options.ClientCertificatePath);
        var trustedServerCertificate = LoadCertificate(options.TrustedServerCertificatePath);
        var handler = new SocketsHttpHandler
        {
            ConnectCallback = async (context, cancellationToken) =>
            {
                var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                try
                {
                    await socket.ConnectAsync(
                        new UnixDomainSocketEndPoint(options.SocketPath),
                        cancellationToken);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            },
            SslOptions = new SslClientAuthenticationOptions
            {
                TargetHost = "bigbrain-sentinel",
                EnabledSslProtocols = SslProtocols.Tls13,
                ApplicationProtocols = [SslApplicationProtocol.Http2],
                ClientCertificates = new X509CertificateCollection { clientCertificate },
                RemoteCertificateValidationCallback = (_, certificate, _, _) =>
                    certificate is not null
                    && CryptographicOperations.FixedTimeEquals(
                        certificate.GetRawCertData(),
                        trustedServerCertificate.RawData)
            }
        };

        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://bigbrain-sentinel", UriKind.Absolute),
            DefaultRequestVersion = HttpVersion.Version20,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact,
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    private static X509Certificate2 LoadCertificate(string path) =>
        X509CertificateLoader.LoadPkcs12FromFile(
            path,
            password: null,
            X509KeyStorageFlags.EphemeralKeySet);
}
