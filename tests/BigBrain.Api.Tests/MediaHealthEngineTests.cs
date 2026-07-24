using BigBrain.Api.Media;

namespace BigBrain.Api.Tests;

public sealed class MediaHealthEngineTests
{
    [Theory]
    [InlineData(MediaStatuses.Online, 100, "Everything looks great", "excellent")]
    [InlineData(MediaStatuses.Degraded, 50, "Some services need attention", "actionRecommended")]
    [InlineData(MediaStatuses.Unavailable, 0, "Immediate attention is recommended", "critical")]
    public void AssessmentMapsServiceStateToScoreAndPresentation(
        string status,
        int expectedScore,
        string expectedSummary,
        string expectedLevel)
    {
        var assessment = new MediaHealthEngine().Assess(
            [Service(status)],
            Sonarr(),
            Radarr(),
            Prowlarr(),
            QBittorrent());

        Assert.Equal(expectedScore, assessment.Score);
        Assert.Equal(expectedSummary, assessment.Summary);
        Assert.Equal(expectedLevel, assessment.StatusLevel);
    }

    [Fact]
    public void AssessmentAppliesOperationalRulesAndClampsScore()
    {
        var warnings = new[] { new MediaHealthWarning("Test", "Warning") };
        var assessment = new MediaHealthEngine().Assess(
            [Service(MediaStatuses.Online)],
            Sonarr(warnings),
            Radarr(warnings),
            Prowlarr(warnings, enabled: 2, online: 1),
            QBittorrent(active: 1, speed: 0, freeSpace: 1024));

        Assert.Equal(58, assessment.Score);
        Assert.Equal("Some services need attention", assessment.Summary);
        Assert.Equal("actionRecommended", assessment.StatusLevel);
    }

    [Fact]
    public void AssessmentReturnsNeutralStateWhenNothingIsConfigured()
    {
        var assessment = new MediaHealthEngine().Assess(
            [Service(MediaStatuses.NotConfigured) with { IsConfigured = false }],
            Sonarr(),
            Radarr(),
            Prowlarr(),
            QBittorrent());

        Assert.Equal(0, assessment.Score);
        Assert.Equal("Configure media services to calculate health.", assessment.Summary);
        Assert.Equal("notConfigured", assessment.StatusLevel);
    }

    private static MediaServiceStatus Service(string status) =>
        new("Test", status, "1.0", 1, DateTimeOffset.UtcNow, null, true);

    private static SonarrOverview Sonarr(IReadOnlyList<MediaHealthWarning>? warnings = null) =>
        new(Service(MediaStatuses.Online), 0, 0, 0, 0, [], [], [], warnings ?? []);

    private static RadarrOverview Radarr(IReadOnlyList<MediaHealthWarning>? warnings = null) =>
        new(Service(MediaStatuses.Online), 0, 0, 0, 0, 0, [], [], warnings ?? []);

    private static ProwlarrOverview Prowlarr(
        IReadOnlyList<MediaHealthWarning>? warnings = null,
        int enabled = 0,
        int online = 0) =>
        new(Service(MediaStatuses.Online), 0, enabled, online, 0, [], [], [], warnings ?? []);

    private static QBittorrentOverview QBittorrent(int active = 0, long speed = 0, long? freeSpace = null) =>
        new(Service(MediaStatuses.Online), active, 0, 0, speed, 0, null, null, 0, 0, freeSpace, []);
}
