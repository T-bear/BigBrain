namespace BigBrain.Sentinel;

public interface IHostUptimeReader
{
    double ReadUptimeSeconds();
}

public sealed class HostUptimeReader : IHostUptimeReader
{
    public double ReadUptimeSeconds() => Environment.TickCount64 / 1000d;
}
