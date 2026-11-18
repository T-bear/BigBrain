using System.Net;

namespace BigBrain.Api;

// Defense in depth for the currently inventoried Docker Media bridge and host gateway.
// The network-bound policy must be revalidated whenever Docker network addressing changes.
public sealed class MediaIngressGuard
{
    private readonly IPNetwork[] _denied;

    private MediaIngressGuard(IPNetwork[] denied) => _denied = denied;

    public static MediaIngressGuard Parse(string? deniedCidrs)
    {
        if (deniedCidrs is null) return new MediaIngressGuard([]);
        var cidrs = deniedCidrs.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (cidrs.Length == 0) throw new InvalidOperationException("Media ingress policy is empty.");
        var networks = cidrs.Select(value =>
            IPNetwork.TryParse(value, out var network)
                ? network
                : throw new InvalidOperationException("Invalid Media ingress network.")).ToArray();
        return new MediaIngressGuard(networks);
    }

    public bool IsDenied(IPAddress? remoteAddress)
    {
        if (_denied.Length == 0) return false;
        if (remoteAddress is null) return true;
        if (remoteAddress.IsIPv4MappedToIPv6) remoteAddress = remoteAddress.MapToIPv4();
        return _denied.Any(network => network.Contains(remoteAddress));
    }
}
