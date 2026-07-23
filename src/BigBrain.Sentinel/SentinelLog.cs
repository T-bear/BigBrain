namespace BigBrain.Sentinel;

internal static partial class SentinelLog
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Sentinel bootstrap initialized with version {SentinelVersion} and {CapabilityCount} capabilities.")]
    public static partial void BootstrapInitialized(
        ILogger logger,
        string sentinelVersion,
        int capabilityCount);
}
