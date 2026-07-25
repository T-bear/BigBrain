namespace BigBrain.Api.Tests;

public sealed class MediaArchitectureTests
{
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
