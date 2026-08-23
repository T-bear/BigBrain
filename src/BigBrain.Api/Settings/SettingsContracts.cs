namespace BigBrain.Api.Settings;

public sealed class SettingsOptions
{
    public const string SectionName = "Settings";
    public string DatabasePath { get; init; } = "data/settings.db";
}

public sealed record ThemeSetting(string Theme, bool Configured = true);
public sealed record AudiobookLanguageSetting(string PreferredLanguage, string FallbackLanguage);

public static class ThemeIds
{
    public const string Default = "obsidian-gold";
    public static readonly HashSet<string> All = [Default, "arctic-wind", "forest-night", "bigbrain-dark", "bigbrain-light", "bigbrain-obsidian-gold"];
}
