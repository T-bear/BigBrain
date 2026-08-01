using System.Text.Json;
using System.Text.Json.Nodes;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.MealPlanner;

public sealed class MealPlannerStore : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string connectionString;
    private readonly SemaphoreSlim gate = new(1, 1);
    public bool IsAvailable { get; private set; }

    public MealPlannerStore(MealPlannerOptions options)
    {
        try
        {
            var fullPath = Path.GetFullPath(options.DatabasePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            connectionString = new SqliteConnectionStringBuilder { DataSource = fullPath, ForeignKeys = true }.ToString();
            Initialize();
            IsAvailable = true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SqliteException)
        {
            connectionString = string.Empty;
            IsAvailable = false;
        }
    }

    public async Task<IReadOnlyList<MealTag>> GetTagsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Category, CreatedAtUtc, IsProtected FROM Tags ORDER BY Category, Name";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<MealTag>();
        while (await reader.ReadAsync(cancellationToken)) result.Add(ReadTag(reader));
        return result;
    }

    public async Task<MealTag> CreateTagAsync(string name, string category, CancellationToken cancellationToken)
    {
        var tag = new MealTag(Guid.NewGuid().ToString("N"), name, category, DateTimeOffset.UtcNow, false);
        try
        {
            await ExecuteAsync(
                "INSERT INTO Tags (Id, Name, Category, CreatedAtUtc, IsProtected) VALUES ($id, $name, $category, $created, 0)",
                command =>
                {
                    command.Parameters.AddWithValue("$id", tag.Id);
                    command.Parameters.AddWithValue("$name", tag.Name);
                    command.Parameters.AddWithValue("$category", tag.Category);
                    command.Parameters.AddWithValue("$created", tag.CreatedAtUtc.ToString("O"));
                }, cancellationToken);
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            throw new MealPlannerException(MealPlannerErrorCodes.TagAlreadyExists,
                "A tag with the same name and category already exists.", StatusCodes.Status409Conflict);
        }
        return tag;
    }

    public async Task DeleteTagAsync(string id, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            await using var connection = await OpenAsync(cancellationToken);
            await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
            await using var find = connection.CreateCommand();
            find.Transaction = transaction;
            find.CommandText = "SELECT IsProtected FROM Tags WHERE Id = $id";
            find.Parameters.AddWithValue("$id", id);
            var protectedValue = await find.ExecuteScalarAsync(cancellationToken);
            if (protectedValue is null) throw NotFound(MealPlannerErrorCodes.TagNotFound, "Tag was not found.");
            if (Convert.ToInt32(protectedValue, CultureInfo.InvariantCulture) == 1)
                throw new MealPlannerException(MealPlannerErrorCodes.ProtectedTag, "Default tags cannot be deleted.", StatusCodes.Status409Conflict);

            var meals = await ReadMealsAsync(connection, transaction, cancellationToken);
            foreach (var meal in meals.Where(meal => meal.TagIds.Contains(id, StringComparer.Ordinal)))
            {
                await using var update = connection.CreateCommand();
                update.Transaction = transaction;
                update.CommandText = "UPDATE Meals SET TagIdsJson = $tags, UpdatedAtUtc = $updated WHERE Id = $id";
                update.Parameters.AddWithValue("$tags", Serialize(meal.TagIds.Where(tagId => tagId != id).ToArray()));
                update.Parameters.AddWithValue("$updated", DateTimeOffset.UtcNow.ToString("O"));
                update.Parameters.AddWithValue("$id", meal.Id);
                await update.ExecuteNonQueryAsync(cancellationToken);
            }
            await using var delete = connection.CreateCommand();
            delete.Transaction = transaction;
            delete.CommandText = "DELETE FROM Tags WHERE Id = $id";
            delete.Parameters.AddWithValue("$id", id);
            await delete.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        finally { gate.Release(); }
    }

    public async Task<IReadOnlyList<Meal>> GetMealsAsync(IReadOnlyList<string> tagIds, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var meals = await ReadMealsAsync(connection, null, cancellationToken);
        return tagIds.Count == 0
            ? meals
            : meals.Where(meal => tagIds.All(tagId => meal.TagIds.Contains(tagId, StringComparer.Ordinal))).ToArray();
    }

    public async Task<Meal> CreateMealAsync(string name, IReadOnlyList<string> tagIds, CancellationToken cancellationToken)
    {
        await EnsureTagsExistAsync(tagIds, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var meal = new Meal(Guid.NewGuid().ToString("N"), name, tagIds.Distinct(StringComparer.Ordinal).ToArray(), now, now);
        await ExecuteAsync(
            "INSERT INTO Meals (Id, Name, TagIdsJson, CreatedAtUtc, UpdatedAtUtc) VALUES ($id, $name, $tags, $created, $updated)",
            command => AddMealParameters(command, meal), cancellationToken);
        return meal;
    }

    public async Task<Meal> UpdateMealAsync(string id, string name, IReadOnlyList<string> tagIds, CancellationToken cancellationToken)
    {
        await EnsureTagsExistAsync(tagIds, cancellationToken);
        var existing = (await GetMealsAsync([], cancellationToken)).SingleOrDefault(meal => meal.Id == id)
            ?? throw NotFound(MealPlannerErrorCodes.MealNotFound, "Meal was not found.");
        var meal = existing with { Name = name, TagIds = tagIds.Distinct(StringComparer.Ordinal).ToArray(), UpdatedAtUtc = DateTimeOffset.UtcNow };
        await ExecuteAsync(
            "UPDATE Meals SET Name = $name, TagIdsJson = $tags, UpdatedAtUtc = $updated WHERE Id = $id",
            command =>
            {
                command.Parameters.AddWithValue("$id", meal.Id);
                command.Parameters.AddWithValue("$name", meal.Name);
                command.Parameters.AddWithValue("$tags", Serialize(meal.TagIds));
                command.Parameters.AddWithValue("$updated", meal.UpdatedAtUtc.ToString("O"));
            }, cancellationToken);
        return meal;
    }

    public async Task<SeedExampleMealsResponse> SeedExampleMealsAsync(
        IReadOnlyList<ExampleMealDefinition> examples,
        CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            await using var connection = await OpenAsync(cancellationToken);
            await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
            var existingNames = (await ReadMealsAsync(connection, transaction, cancellationToken))
                .Select(meal => meal.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var createdCount = 0;
            foreach (var example in examples)
            {
                if (!existingNames.Add(example.Name)) continue;
                var now = DateTimeOffset.UtcNow;
                var meal = new Meal(Guid.NewGuid().ToString("N"), example.Name, example.TagIds, now, now);
                await using var insert = connection.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText = "INSERT INTO Meals (Id, Name, TagIdsJson, CreatedAtUtc, UpdatedAtUtc) VALUES ($id, $name, $tags, $created, $updated)";
                AddMealParameters(insert, meal);
                await insert.ExecuteNonQueryAsync(cancellationToken);
                createdCount++;
            }
            await transaction.CommitAsync(cancellationToken);
            return new(createdCount, examples.Count - createdCount);
        }
        finally { gate.Release(); }
    }

    public async Task DeleteMealAsync(string id, CancellationToken cancellationToken)
    {
        var changed = await ExecuteAsync("DELETE FROM Meals WHERE Id = $id", command => command.Parameters.AddWithValue("$id", id), cancellationToken);
        if (changed == 0) throw NotFound(MealPlannerErrorCodes.MealNotFound, "Meal was not found.");
    }

    public async Task<IReadOnlyList<MealSchedule>> GetSchedulesAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, StartDate, EndDate, CreatedAtUtc, UpdatedAtUtc, Title, GenerationVersion, DaysJson FROM Schedules ORDER BY StartDate DESC, CreatedAtUtc DESC";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<MealSchedule>();
        while (await reader.ReadAsync(cancellationToken)) result.Add(ReadSchedule(reader));
        return result;
    }

    public async Task<MealSchedule> GetScheduleAsync(string id, CancellationToken cancellationToken) =>
        (await GetSchedulesAsync(cancellationToken)).SingleOrDefault(schedule => schedule.Id == id)
        ?? throw NotFound(MealPlannerErrorCodes.ScheduleNotFound, "Schedule was not found.");

    public async Task<MealSchedule> SaveScheduleAsync(MealSchedule schedule, CancellationToken cancellationToken)
    {
        await ExecuteAsync(
            "INSERT INTO Schedules (Id, StartDate, EndDate, CreatedAtUtc, UpdatedAtUtc, Title, GenerationVersion, DaysJson) VALUES ($id, $start, $end, $created, $updated, $title, $version, $days)",
            command => AddScheduleParameters(command, schedule), cancellationToken);
        return schedule;
    }

    public async Task<MealSchedule> UpdateScheduleAsync(MealSchedule schedule, CancellationToken cancellationToken)
    {
        var changed = await ExecuteAsync(
            "UPDATE Schedules SET UpdatedAtUtc = $updated, DaysJson = $days WHERE Id = $id",
            command =>
            {
                command.Parameters.AddWithValue("$id", schedule.Id);
                command.Parameters.AddWithValue("$updated", schedule.UpdatedAtUtc.ToString("O"));
                command.Parameters.AddWithValue("$days", Serialize(schedule.Days));
            }, cancellationToken);
        if (changed == 0) throw NotFound(MealPlannerErrorCodes.ScheduleNotFound, "Schedule was not found.");
        return schedule;
    }

    public async Task DeleteScheduleAsync(string id, CancellationToken cancellationToken)
    {
        var changed = await ExecuteAsync("DELETE FROM Schedules WHERE Id = $id", command => command.Parameters.AddWithValue("$id", id), cancellationToken);
        if (changed == 0) throw NotFound(MealPlannerErrorCodes.ScheduleNotFound, "Schedule was not found.");
    }

    private void Initialize()
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS Tags (Id TEXT PRIMARY KEY, Name TEXT NOT NULL, Category TEXT NOT NULL, CreatedAtUtc TEXT NOT NULL, IsProtected INTEGER NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_Tags_Name_Category ON Tags(Name, Category);
            CREATE TABLE IF NOT EXISTS Meals (Id TEXT PRIMARY KEY, Name TEXT NOT NULL, TagIdsJson TEXT NOT NULL, CreatedAtUtc TEXT NOT NULL, UpdatedAtUtc TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS Schedules (Id TEXT PRIMARY KEY, StartDate TEXT NOT NULL, EndDate TEXT NOT NULL, CreatedAtUtc TEXT NOT NULL, UpdatedAtUtc TEXT NOT NULL, Title TEXT NULL, GenerationVersion INTEGER NOT NULL, DaysJson TEXT NOT NULL);
            """;
        command.ExecuteNonQuery();
        var schemaVersion = ReadSchemaVersion(connection);
        if (schemaVersion > 2) throw new IOException("Meal planner database schema is newer than this application supports.");
        if (schemaVersion == 1) MigrateSchedulesToMealTypes(connection);
        if (schemaVersion < 2) SetSchemaVersion(connection, 2);
        SeedTag(connection, "portion-3-4", "3–4 personer", MealPlannerTagCategories.Portion);
        SeedTag(connection, "portion-6", "6 personer", MealPlannerTagCategories.Portion);
        SeedTag(connection, "occasion-friday", "Fredagsmat", MealPlannerTagCategories.Occasion);
        SeedTag(connection, "occasion-easy", "Lättlagat", MealPlannerTagCategories.Occasion);
        SeedTag(connection, "occasion-weekend", "Helgmat", MealPlannerTagCategories.Occasion);
        SeedTag(connection, "meal-type-lunch", "Lunch", MealPlannerTagCategories.MealType);
    }

    private static int ReadSchemaVersion(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version";
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private static void SetSchemaVersion(SqliteConnection connection, int version)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA user_version={version}";
        command.ExecuteNonQuery();
    }

    private static void MigrateSchedulesToMealTypes(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction();
        var schedules = new List<(string Id, string DaysJson)>();
        using (var read = connection.CreateCommand())
        {
            read.Transaction = transaction;
            read.CommandText = "SELECT Id, DaysJson FROM Schedules";
            using var reader = read.ExecuteReader();
            while (reader.Read()) schedules.Add((reader.GetString(0), reader.GetString(1)));
        }
        foreach (var schedule in schedules)
        {
            var days = JsonNode.Parse(schedule.DaysJson)?.AsArray()
                ?? throw new IOException("Meal planner schedule data is invalid.");
            var changed = false;
            foreach (var node in days)
            {
                var day = node?.AsObject() ?? throw new IOException("Meal planner schedule data is invalid.");
                if (day.ContainsKey("mealType")) continue;
                day["mealType"] = MealPlannerMealTypes.Dinner;
                changed = true;
            }
            if (!changed) continue;
            using var update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText = "UPDATE Schedules SET DaysJson = $days WHERE Id = $id";
            update.Parameters.AddWithValue("$days", days.ToJsonString(JsonOptions));
            update.Parameters.AddWithValue("$id", schedule.Id);
            update.ExecuteNonQuery();
        }
        using var version = connection.CreateCommand();
        version.Transaction = transaction;
        version.CommandText = "PRAGMA user_version=2";
        version.ExecuteNonQuery();
        transaction.Commit();
    }

    private static void SeedTag(SqliteConnection connection, string id, string name, string category)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT OR IGNORE INTO Tags (Id, Name, Category, CreatedAtUtc, IsProtected) VALUES ($id, $name, $category, $created, 1)";
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$category", category);
        command.Parameters.AddWithValue("$created", DateTimeOffset.UtcNow.ToString("O"));
        command.ExecuteNonQuery();
    }

    private async Task EnsureTagsExistAsync(IReadOnlyList<string> tagIds, CancellationToken cancellationToken)
    {
        if (tagIds.Count == 0) return;
        var existing = (await GetTagsAsync(cancellationToken)).Select(tag => tag.Id).ToHashSet(StringComparer.Ordinal);
        if (tagIds.Any(tagId => !existing.Contains(tagId))) throw NotFound(MealPlannerErrorCodes.TagNotFound, "One or more tags were not found.");
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        if (!IsAvailable) throw new MealPlannerUnavailableException();
        try
        {
            var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch (SqliteException) { IsAvailable = false; throw new MealPlannerUnavailableException(); }
    }

    private async Task<int> ExecuteAsync(string sql, Action<SqliteCommand> parameters, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        parameters(command);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<IReadOnlyList<Meal>> ReadMealsAsync(SqliteConnection connection, SqliteTransaction? transaction, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT Id, Name, TagIdsJson, CreatedAtUtc, UpdatedAtUtc FROM Meals ORDER BY Name";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<Meal>();
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new(reader.GetString(0), reader.GetString(1), Deserialize<string[]>(reader.GetString(2)), DateTimeOffset.Parse(reader.GetString(3), CultureInfo.InvariantCulture), DateTimeOffset.Parse(reader.GetString(4), CultureInfo.InvariantCulture)));
        return result;
    }

    private static MealTag ReadTag(SqliteDataReader reader) =>
        new(reader.GetString(0), reader.GetString(1), reader.GetString(2), DateTimeOffset.Parse(reader.GetString(3), CultureInfo.InvariantCulture), reader.GetBoolean(4));

    private static MealSchedule ReadSchedule(SqliteDataReader reader) =>
        new(reader.GetString(0), DateOnly.Parse(reader.GetString(1), CultureInfo.InvariantCulture), DateOnly.Parse(reader.GetString(2), CultureInfo.InvariantCulture), DateTimeOffset.Parse(reader.GetString(3), CultureInfo.InvariantCulture), DateTimeOffset.Parse(reader.GetString(4), CultureInfo.InvariantCulture), Deserialize<ScheduleDay[]>(reader.GetString(7)), reader.IsDBNull(5) ? null : reader.GetString(5), reader.GetInt32(6));

    private static void AddMealParameters(SqliteCommand command, Meal meal)
    {
        command.Parameters.AddWithValue("$id", meal.Id);
        command.Parameters.AddWithValue("$name", meal.Name);
        command.Parameters.AddWithValue("$tags", Serialize(meal.TagIds));
        command.Parameters.AddWithValue("$created", meal.CreatedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$updated", meal.UpdatedAtUtc.ToString("O"));
    }

    private static void AddScheduleParameters(SqliteCommand command, MealSchedule schedule)
    {
        command.Parameters.AddWithValue("$id", schedule.Id);
        command.Parameters.AddWithValue("$start", schedule.StartDate.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$end", schedule.EndDate.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$created", schedule.CreatedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$updated", schedule.UpdatedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$title", (object?)schedule.Title ?? DBNull.Value);
        command.Parameters.AddWithValue("$version", schedule.GenerationVersion);
        command.Parameters.AddWithValue("$days", Serialize(schedule.Days));
    }

    private static MealPlannerException NotFound(string code, string message) => new(code, message, StatusCodes.Status404NotFound);
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);
    private static T Deserialize<T>(string value) => JsonSerializer.Deserialize<T>(value, JsonOptions)!;

    public void Dispose()
    {
        gate.Dispose();
    }
}
