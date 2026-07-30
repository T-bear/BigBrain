using BigBrain.Sentinel.Contracts;

namespace BigBrain.Sentinel;

public static class SentinelSnapshotRequestValidator
{
    private static readonly SentinelSnapshotArguments ExpectedArguments =
        SentinelSnapshotRequest.CreateArguments();

    public static SentinelProtocolError? Validate(
        SentinelCapabilityRequest request,
        ICapabilityRegistry capabilities,
        SentinelProtocolOptions options)
    {
        if (string.IsNullOrWhiteSpace(request.MessageId) || request.MessageId.Length > 128)
        {
            return Invalid("The message ID is invalid.");
        }

        if (!string.Equals(request.NodeId, options.NodeId, StringComparison.Ordinal))
        {
            return Invalid("The target node is invalid.");
        }

        if (!capabilities.Contains(request.Capability, request.Version))
        {
            return new SentinelProtocolError(
                "CAPABILITY_UNKNOWN",
                "The requested capability is not registered.",
                false);
        }

        if (!ArgumentsMatch(request.Arguments, ExpectedArguments))
        {
            return Invalid("The System Metrics request contract is invalid.");
        }

        return null;
    }

    private static bool ArgumentsMatch(
        SentinelSnapshotArguments actual,
        SentinelSnapshotArguments expected)
    {
        if (actual.Sections.Count != expected.Sections.Count)
        {
            return false;
        }

        for (var index = 0; index < expected.Sections.Count; index++)
        {
            var actualSection = actual.Sections[index];
            var expectedSection = expected.Sections[index];
            if (!string.Equals(actualSection.Capability, expectedSection.Capability, StringComparison.Ordinal)
                || !actualSection.Fields.SequenceEqual(expectedSection.Fields, StringComparer.Ordinal)
                || !string.Equals(
                    actualSection.ResourceSelector?.FilesystemSet,
                    expectedSection.ResourceSelector?.FilesystemSet,
                    StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static SentinelProtocolError Invalid(string message) =>
        new("PROTOCOL_INVALID", message, false);
}
