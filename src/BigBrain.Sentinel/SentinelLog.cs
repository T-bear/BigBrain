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

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Sentinel capability {CapabilityName}@{CapabilityVersion} completed with outcome {Outcome}.")]
    public static partial void CapabilityCompleted(
        ILogger logger,
        string capabilityName,
        int capabilityVersion,
        string outcome);
}
