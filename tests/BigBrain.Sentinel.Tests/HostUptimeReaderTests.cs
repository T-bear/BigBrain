namespace BigBrain.Sentinel.Tests;

public sealed class HostUptimeReaderTests
{
    [Fact]
    public void ReaderReturnsNonNegativeMonotonicOperatingSystemUptime()
    {
        var reader = new HostUptimeReader();

        var first = reader.ReadUptimeSeconds();
        var second = reader.ReadUptimeSeconds();

        Assert.True(first >= 0);
        Assert.True(second >= first);
    }
}
