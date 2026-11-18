using System.Net;
using BigBrain.Api;

namespace BigBrain.Api.Tests;

public sealed class MediaIngressGuardTests
{
    // Documentation-only TEST-NET prefixes; never use actual home network addresses.
    private static readonly MediaIngressGuard Guard =
        MediaIngressGuard.Parse("198.51.100.0/24,203.0.113.1/32");

    [Theory]
    [InlineData("198.51.100.2", true)]
    [InlineData("198.51.100.254", true)]
    [InlineData("203.0.113.1", true)]
    [InlineData("::ffff:198.51.100.20", true)]
    [InlineData("192.0.2.2", false)]
    [InlineData("203.0.113.2", false)]
    public void RejectsConfiguredOriginsButPreservesOthers(string address, bool denied)
    {
        Assert.Equal(denied, Guard.IsDenied(IPAddress.Parse(address)));
    }

    [Fact]
    public void MissingIpFailsClosedWhenProtectionConfigured() => Assert.True(Guard.IsDenied(null));

    [Fact]
    public void EmptyPolicyIsRejected() =>
        Assert.Throws<InvalidOperationException>(() => MediaIngressGuard.Parse(" , "));

    [Fact]
    public void InvalidPolicyIsRejected() =>
        Assert.Throws<InvalidOperationException>(() => MediaIngressGuard.Parse("not-a-network"));
}
