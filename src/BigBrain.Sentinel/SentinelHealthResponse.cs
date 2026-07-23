namespace BigBrain.Sentinel;

public sealed record SentinelHealthResponse(
    string Status,
    string Version,
    int CapabilityCount,
    DateTimeOffset CheckedAtUtc);
