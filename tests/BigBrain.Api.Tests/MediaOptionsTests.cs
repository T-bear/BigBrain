using BigBrain.Api.Media;
using Microsoft.Extensions.Configuration;

namespace BigBrain.Api.Tests;

public sealed class MediaOptionsTests
{
    [Fact]
    public void UserIdBindsFromEnvironmentStyleKey()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Media:Jellyfin:UserId"] = "user-id-from-runtime"
            })
            .Build();

        var options = configuration.GetSection(MediaOptions.SectionName).Get<MediaOptions>();

        Assert.NotNull(options);
        Assert.Equal("user-id-from-runtime", options.Jellyfin.UserId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EnabledSmartShuffleRequiresUserId(string? userId)
    {
        var options = new MediaOptions
        {
            Jellyfin = new MediaApiKeyOptions("http://jellyfin:8096") { UserId = userId },
            SmartShuffle = new SmartShuffleOptions { Enabled = true }
        };

        Assert.False(MediaOptions.IsValid(options));
    }

    [Fact]
    public void DisabledSmartShuffleDoesNotRequireUserId()
    {
        var options = new MediaOptions
        {
            Jellyfin = new MediaApiKeyOptions("http://jellyfin:8096"),
            SmartShuffle = new SmartShuffleOptions { Enabled = false }
        };

        Assert.True(MediaOptions.IsValid(options));
    }

    [Fact]
    public void PublicMediaDtosDoNotExposeUserId()
    {
        var publicContracts = new[]
        {
            typeof(MediaOverview),
            typeof(MediaServiceStatus),
            typeof(MediaServiceLink),
            typeof(MediaSearchResponse),
            typeof(MediaJobsResponse),
            typeof(MediaPlayResponse)
        }.SelectMany(type => type.GetProperties()).Select(property => property.Name);

        Assert.DoesNotContain("UserId", publicContracts);
    }

    [Fact]
    public void OptionsValidationDoesNotIncludeUserIdValue()
    {
        const string secretUserId = "never-log-this-user-id";
        var options = new MediaOptions
        {
            Jellyfin = new MediaApiKeyOptions("http://jellyfin:8096") { UserId = secretUserId },
            SmartShuffle = new SmartShuffleOptions { Enabled = true }
        };

        Assert.True(MediaOptions.IsValid(options));
        Assert.DoesNotContain(secretUserId, "Media configuration is invalid; Smart Shuffle requires a Jellyfin UserId when enabled.");
    }
}
