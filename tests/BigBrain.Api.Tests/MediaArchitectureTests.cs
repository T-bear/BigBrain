using BigBrain.Api.Media;

namespace BigBrain.Api.Tests;

public sealed class MediaArchitectureTests
{
    private static readonly string[] FrontendMediaDirectories = ["media-search", "media-jobs"];

    [Fact]
    public void MediaProductionCodeHasNoHostOrDockerIntegration()
    {
        var repositoryRoot = FindRepositoryRoot();
        var mediaFiles = Directory.GetFiles(
            Path.Combine(repositoryRoot.FullName, "src", "BigBrain.Api", "Media"),
            "*.cs",
            SearchOption.TopDirectoryOnly);
        var files = mediaFiles.Append(
            Path.Combine(repositoryRoot.FullName, "src", "BigBrain.Modules", "MediaModule.cs"));
        string[] forbiddenTokens =
        [
            "docker.sock",
            "/var/run/docker",
            "\"/proc",
            "\"/sys",
            "Process.Start",
            "System.Diagnostics.Process",
            "DriveInfo",
            "File.Read",
            "File.Write",
            "Directory.Enumerate",
            "api/v2/auth/login",
            "QBittorrent.Username",
            "QBittorrent.Password"
        ];

        foreach (var file in files)
        {
            var source = File.ReadAllText(file);
            Assert.All(
                forbiddenTokens,
                token => Assert.DoesNotContain(token, source, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void MediaContractsRemainSeparatedAndExternalWritesStayInArrAdapter()
    {
        Assert.False(typeof(IMediaLookupProvider).IsAssignableFrom(typeof(ISonarrClient)));
        Assert.False(typeof(IMediaLookupProvider).IsAssignableFrom(typeof(IRadarrClient)));
        Assert.False(typeof(IMediaSearchProvider).IsAssignableFrom(typeof(IMediaLookupProvider)));
        Assert.False(typeof(IMediaRequestService).IsAssignableFrom(typeof(IMediaLookupService)));
        Assert.False(typeof(IMediaJobsService).IsAssignableFrom(typeof(IMediaLookupService)));
        Assert.False(typeof(IMediaJobsService).IsAssignableFrom(typeof(IMediaRequestService)));

        var root = FindRepositoryRoot();
        var mediaDirectory = Path.Combine(root.FullName, "src", "BigBrain.Api", "Media");
        var filesWithPost = Directory.GetFiles(mediaDirectory, "*.cs")
            .Where(file => File.ReadAllText(file).Contains("HttpMethod.Post", StringComparison.Ordinal))
            .Select(file => Path.GetFileName(file)!)
            .ToArray();
        Assert.Equal(["ArrClients.cs"], filesWithPost);
        var arrSource = File.ReadAllText(Path.Combine(mediaDirectory, "ArrClients.cs"));
        Assert.Equal(2, Count(arrSource, "HttpMethod.Post"));
        Assert.Contains("\"api/v3/series\"", arrSource, StringComparison.Ordinal);
        Assert.Contains("\"api/v3/movie\"", arrSource, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpMethod.Put", arrSource, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpMethod.Patch", arrSource, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpMethod.Delete", arrSource, StringComparison.Ordinal);
        var allMediaSource = string.Join(
            Environment.NewLine,
            Directory.GetFiles(mediaDirectory, "*.cs").Select(File.ReadAllText));
        string[] forbiddenWriteTokens =
        [
            "HttpMethod.Put", "HttpMethod.Patch", "HttpMethod.Delete",
            "PostAsync(", "PutAsync(", "PatchAsync(", "DeleteAsync(",
            "\"api/v3/command", "\"api/v3/release"
        ];
        Assert.All(forbiddenWriteTokens, token =>
            Assert.DoesNotContain(token, allMediaSource, StringComparison.OrdinalIgnoreCase));

        var lookupSource = File.ReadAllText(Path.Combine(mediaDirectory, "MediaLookup.cs"));
        Assert.DoesNotContain("HttpMethod.Post", lookupSource, StringComparison.Ordinal);
        var jellyfinSource = File.ReadAllText(Path.Combine(mediaDirectory, "JellyfinClient.cs"));
        Assert.DoesNotContain("HttpMethod.Post", jellyfinSource, StringComparison.Ordinal);
        var jobsSource = File.ReadAllText(Path.Combine(mediaDirectory, "MediaJobs.cs"));
        Assert.DoesNotContain("HttpMethod.Post", jobsSource, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpMethod.Put", jobsSource, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpMethod.Patch", jobsSource, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpMethod.Delete", jobsSource, StringComparison.Ordinal);
    }

    [Fact]
    public void PublicMediaDtosAndFrontendDoNotExposeInternalConfiguration()
    {
        Type[] publicDtos =
        [
            typeof(MediaLookupResponse),
            typeof(MediaLookupProviderResult),
            typeof(MediaLookupResult),
            typeof(MediaAddOptionsResponse),
            typeof(MediaAddOption),
            typeof(MediaRequestPreviewResponse),
            typeof(MediaRequestSummary),
            typeof(MediaRequestConfirmResponse),
            typeof(MediaJob),
            typeof(MediaJobsResponse),
            typeof(MediaLibraryStatusResponse),
            typeof(MediaPlayResponse)
        ];
        string[] forbiddenNames = ["ApiKey", "Password", "BaseUrl", "Path", "InternalUrl", "StackTrace", "Exception"];
        Assert.All(publicDtos, type => Assert.All(
            type.GetProperties(),
            property =>
            {
                if (type == typeof(MediaPlayResponse) && property.Name == nameof(MediaPlayResponse.PlayUrl))
                {
                    return;
                }
                Assert.DoesNotContain(forbiddenNames, name =>
                    property.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
            }));

        var root = FindRepositoryRoot();
        var frontendRoot = Path.Combine(root.FullName, "src", "BigBrain.Web", "src");
        var frontend = FrontendMediaDirectories
            .SelectMany(directory => Directory.GetFiles(
                Path.Combine(frontendRoot, directory),
                "*.tsx"))
            .Select(File.ReadAllText);
        Assert.All(frontend, source =>
        {
            Assert.DoesNotContain("rootFolderPath", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("HttpMethod", source, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static int Count(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var configuredRoot = Environment.GetEnvironmentVariable("BIGBRAIN_REPOSITORY_ROOT");
        var startPaths = string.IsNullOrWhiteSpace(configuredRoot)
            ? new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory }
            : new[] { configuredRoot, Directory.GetCurrentDirectory(), AppContext.BaseDirectory };
        foreach (var startPath in startPaths)
        {
            var directory = new DirectoryInfo(startPath);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "BigBrain.slnx")))
            {
                directory = directory.Parent;
            }

            if (directory is not null)
            {
                return directory;
            }
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
