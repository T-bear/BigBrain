using System.ComponentModel.DataAnnotations;

namespace BigBrain.Sentinel;

public sealed class SentinelOptions
{
    public const string SectionName = "Sentinel";

    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string ServiceName { get; init; } = "BigBrain Sentinel";

    [Required]
    [RegularExpression("^/[a-z0-9/-]+$")]
    public string HealthPath { get; init; } = "/health";
}
