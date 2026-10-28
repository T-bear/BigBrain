using Microsoft.Data.Sqlite;

namespace BigBrain.Api.MealPlanner;

public sealed partial class MealPlannerStore
{
    // Two source rows in the existing DB. No menu, household or credential data.
    // Deliberately separate from household initialization/IsAvailable: failure disables only fetching.
    public async Task<string?> TryBeginSchoolFetchAsync(string school, DateTimeOffset now,
        DateTimeOffset? verifiedInitialNotBefore, CancellationToken token)
    {
        if (!SchoolMeals.KnownSchool(school) || !IsAvailable) return null;
        try
        {
            await using var connection = new SqliteConnection(SchoolConnectionString());
            await connection.OpenAsync(token);
            await using var schema = connection.CreateCommand();
            schema.CommandText = """
                CREATE TABLE IF NOT EXISTS SchoolMealFetchGate (
                    School TEXT PRIMARY KEY CHECK (School IN ('rosenfeldt', 'musikugglan')),
                    NextAllowedUtcTicks INTEGER NOT NULL CHECK (typeof(NextAllowedUtcTicks) = 'integer' AND NextAllowedUtcTicks >= 0),
                    Pending INTEGER NOT NULL CHECK (Pending IN (0, 1)),
                    Attempt TEXT NULL);
                """;
            await schema.ExecuteNonQueryAsync(token);
            // Unknown history stays absent/blocked. Independently verified bootstrap can seed
            // only a missing row, never reset an existing pending attempt or cooldown.
            if (verifiedInitialNotBefore is not null)
            {
                await using var seed = connection.CreateCommand();
                seed.CommandText = "INSERT OR IGNORE INTO SchoolMealFetchGate VALUES ($school, $next, $pending, NULL)";
                seed.Parameters.AddWithValue("$school", school);
                seed.Parameters.Add("$next", SqliteType.Integer).Value = verifiedInitialNotBefore.Value.UtcTicks;
                seed.Parameters.AddWithValue("$pending", 0);
                await seed.ExecuteNonQueryAsync(token);
            }
            var attempt = Guid.NewGuid().ToString("N");
            await using var reserve = connection.CreateCommand();
            // One atomic, autocommitted write; also excludes other API processes using this DB.
            reserve.CommandText = """
                UPDATE SchoolMealFetchGate SET Pending = 1, Attempt = $attempt
                WHERE School = $school AND Pending = 0 AND Attempt IS NULL
                    AND typeof(NextAllowedUtcTicks) = 'integer' AND NextAllowedUtcTicks >= 0
                    AND NextAllowedUtcTicks <= $now;
                """;
            reserve.Parameters.AddWithValue("$school", school);
            reserve.Parameters.AddWithValue("$attempt", attempt);
            reserve.Parameters.AddWithValue("$now", now.UtcTicks);
            return await reserve.ExecuteNonQueryAsync(token) == 1 ? attempt : null;
        }
        catch (Exception error) when (error is SqliteException or IOException or UnauthorizedAccessException or OperationCanceledException)
        {
            return null;
        }
    }

    public async Task<bool> CompleteSchoolFetchAsync(string school, string attempt, DateTimeOffset nextAllowed,
        CancellationToken token)
    {
        if (!SchoolMeals.KnownSchool(school) || !IsAvailable) return false;
        try
        {
            await using var connection = new SqliteConnection(SchoolConnectionString());
            await connection.OpenAsync(token);
            await using var complete = connection.CreateCommand();
            complete.CommandText = """
                UPDATE SchoolMealFetchGate SET NextAllowedUtcTicks = $next, Pending = 0, Attempt = NULL
                WHERE School = $school AND Pending = 1 AND Attempt = $attempt;
                """;
            complete.Parameters.AddWithValue("$school", school);
            complete.Parameters.AddWithValue("$attempt", attempt);
            complete.Parameters.AddWithValue("$next", nextAllowed.UtcTicks);
            return await complete.ExecuteNonQueryAsync(token) == 1;
        }
        catch (Exception error) when (error is SqliteException or IOException or UnauthorizedAccessException or OperationCanceledException)
        {
            return false;
        }
    }

    private string SchoolConnectionString() => new SqliteConnectionStringBuilder(connectionString)
    {
        Mode = SqliteOpenMode.ReadWrite,
        DefaultTimeout = 1
    }.ToString();
}
