namespace BigBrain.Api.Settings;

public sealed class SettingsOptions
{
    public const string SectionName = "Settings";
    public string DatabasePath { get; init; } = "data/settings.db";
}

public sealed record ThemeSetting(string Theme, bool Configured = true);

public static class ThemeIds
{
    public const string Default = "bigbrain-dark";
    public static readonly HashSet<string> All = [Default, "bigbrain-light", "bigbrain-obsidian-gold"];
}
