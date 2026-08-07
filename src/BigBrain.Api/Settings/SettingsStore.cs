using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Settings;

public sealed class SettingsStore : IDisposable
{
    private readonly string connectionString;
    private readonly SemaphoreSlim gate = new(1, 1);

    public SettingsStore(SettingsOptions options)
    {
        var path = Path.GetFullPath(options.DatabasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ToString();
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE IF NOT EXISTS Settings(Key TEXT PRIMARY KEY,Value TEXT NOT NULL);";
        command.ExecuteNonQuery();
    }

    public async Task<ThemeSetting> GetThemeAsync(CancellationToken token)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(token);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Value FROM Settings WHERE Key='theme'";
        var value = await command.ExecuteScalarAsync(token) as string;
        return ThemeIds.All.Contains(value ?? "") ? new(value!, true) : new(ThemeIds.Default, false);
    }

    public async Task<ThemeSetting> SetThemeAsync(string? theme, CancellationToken token)
    {
        if (!ThemeIds.All.Contains(theme ?? "")) throw new ArgumentException("Temat stöds inte.", nameof(theme));
        await gate.WaitAsync(token);
        try
        {
            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(token);
            await using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO Settings(Key,Value) VALUES('theme',$value) ON CONFLICT(Key) DO UPDATE SET Value=excluded.Value";
            command.Parameters.AddWithValue("$value", theme!);
            await command.ExecuteNonQueryAsync(token);
            return new(theme!);
        }
        finally { gate.Release(); }
    }

    public void Dispose() => gate.Dispose();
}
