using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.ShoppingList;

public sealed partial class ShoppingListStore : IDisposable
{
    private const int SchemaVersion = 1;
    private readonly string connectionString = string.Empty;
    private readonly SemaphoreSlim gate = new(1, 1);
    public bool IsAvailable { get; private set; }

    public ShoppingListStore(ShoppingListOptions options)
    {
        try
        {
            var fullPath = Path.GetFullPath(options.DatabasePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            connectionString = new SqliteConnectionStringBuilder { DataSource = fullPath, ForeignKeys = true }.ToString();
            Initialize();
            IsAvailable = true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SqliteException or InvalidDataException)
        {
            IsAvailable = false;
        }
    }

    public static string CleanName(string? value)
    {
        var cleaned = Spaces().Replace(value?.Trim() ?? string.Empty, " ");
        if (cleaned.Length is < 1 or > 120) throw Invalid("Varans namn måste vara mellan 1 och 120 tecken.");
        return cleaned;
    }

    public static string Normalize(string value) => CleanName(value).Normalize().ToUpper(new CultureInfo("sv-SE"));

    public async Task<ShoppingListSnapshot> GetAsync(CancellationToken token)
    {
        await using var connection = await OpenAsync(token);
        var sessionId = await ScalarAsync(connection, "SELECT Id FROM Sessions WHERE EndedAtUtc IS NULL ORDER BY StartedAtUtc DESC LIMIT 1", token) as string;
        var items = new List<ShoppingItem>();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT i.Id,i.Name,i.NormalizedName,i.Quantity,i.Purchased,i.CreatedAtUtc,i.UpdatedAtUtc,i.SortOrdinal,
                   COALESCE(s.AveragePosition, 999999), COALESCE(s.ObservationCount, 0)
            FROM Items i LEFT JOIN ItemStats s ON s.NormalizedName=i.NormalizedName
            WHERE i.ArchivedAtUtc IS NULL
            ORDER BY i.Purchased, CASE WHEN s.ObservationCount >= 2 THEN s.AveragePosition ELSE 999999 END,
                     i.SortOrdinal, i.CreatedAtUtc, i.Name
            """;
        await using var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token)) items.Add(ReadItem(reader));
        return new(items, sessionId);
    }

    public async Task<ShoppingItem> AddAsync(string rawName, int quantity, CancellationToken token)
    {
        ValidateQuantity(quantity);
        var name = CleanName(rawName); var normalized = Normalize(name); var now = DateTimeOffset.UtcNow;
        await gate.WaitAsync(token);
        try
        {
            await using var connection = await OpenAsync(token);
            await using var duplicate = connection.CreateCommand();
            duplicate.CommandText = "SELECT Purchased FROM Items WHERE NormalizedName=$name AND ArchivedAtUtc IS NULL LIMIT 1";
            duplicate.Parameters.AddWithValue("$name", normalized);
            var existing = await duplicate.ExecuteScalarAsync(token);
            if (existing is not null)
                throw new ShoppingListException(Convert.ToInt32(existing, CultureInfo.InvariantCulture) == 1 ? ShoppingListErrorCodes.PurchasedDuplicate : ShoppingListErrorCodes.Duplicate,
                    Convert.ToInt32(existing, CultureInfo.InvariantCulture) == 1 ? $"{name} ligger redan under Köpta." : $"{name} finns redan på listan.", 409);
            var ordinal = Convert.ToInt32(await ScalarAsync(connection, "SELECT COALESCE(MAX(SortOrdinal),0)+1 FROM Items", token), CultureInfo.InvariantCulture);
            var item = new ShoppingItem(Guid.NewGuid().ToString("N"), name, normalized, quantity, false, now, now, ordinal);
            await using var insert = connection.CreateCommand();
            insert.CommandText = "INSERT INTO Items(Id,Name,NormalizedName,Quantity,Purchased,CreatedAtUtc,UpdatedAtUtc,SortOrdinal) VALUES($id,$name,$normalized,$quantity,0,$now,$now,$ordinal)";
            AddItemParameters(insert, item); await insert.ExecuteNonQueryAsync(token);
            await UpsertAddedStatAsync(connection, normalized, name, token);
            return item;
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19) { throw new ShoppingListException(ShoppingListErrorCodes.Duplicate, $"{name} finns redan på listan.", 409); }
        finally { gate.Release(); }
    }

    public async Task<ShoppingItem> UpdateAsync(string id, string rawName, int quantity, CancellationToken token)
    {
        ValidateQuantity(quantity); var name = CleanName(rawName); var normalized = Normalize(name); var now = DateTimeOffset.UtcNow;
        await using var connection = await OpenAsync(token);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Items SET Name=$name,NormalizedName=$normalized,Quantity=$quantity,UpdatedAtUtc=$now WHERE Id=$id AND ArchivedAtUtc IS NULL";
        command.Parameters.AddWithValue("$id", id); command.Parameters.AddWithValue("$name", name); command.Parameters.AddWithValue("$normalized", normalized);
        command.Parameters.AddWithValue("$quantity", quantity); command.Parameters.AddWithValue("$now", now.ToString("O"));
        try { if (await command.ExecuteNonQueryAsync(token) == 0) throw NotFound(); }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19) { throw new ShoppingListException(ShoppingListErrorCodes.Duplicate, $"{name} finns redan på listan.", 409); }
        return (await GetAsync(token)).Items.Single(x => x.Id == id);
    }

    public async Task<ShoppingItem> SetPurchasedAsync(string id, bool purchased, CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            await using var connection = await OpenAsync(token);
            await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(token);
            var sessionId = purchased ? await EnsureSessionAsync(connection, transaction, token) : null;
            var now = DateTimeOffset.UtcNow;
            await using var update = connection.CreateCommand(); update.Transaction = transaction;
            update.CommandText = "UPDATE Items SET Purchased=$p,UpdatedAtUtc=$now WHERE Id=$id AND ArchivedAtUtc IS NULL";
            update.Parameters.AddWithValue("$p", purchased ? 1 : 0); update.Parameters.AddWithValue("$now", now.ToString("O")); update.Parameters.AddWithValue("$id", id);
            if (await update.ExecuteNonQueryAsync(token) == 0) throw NotFound();
            if (purchased)
            {
                await using var ev = connection.CreateCommand(); ev.Transaction = transaction;
                ev.CommandText = "INSERT OR IGNORE INTO CheckEvents(SessionId,ItemId,NormalizedName,Position,CheckedAtUtc) SELECT $session,Id,NormalizedName,(SELECT COUNT(*)+1 FROM CheckEvents WHERE SessionId=$session),$now FROM Items WHERE Id=$id";
                ev.Parameters.AddWithValue("$session", sessionId!); ev.Parameters.AddWithValue("$id", id); ev.Parameters.AddWithValue("$now", now.ToString("O")); await ev.ExecuteNonQueryAsync(token);
            }
            else { await using var ev = connection.CreateCommand(); ev.Transaction = transaction; ev.CommandText = "DELETE FROM CheckEvents WHERE ItemId=$id AND SessionId IN (SELECT Id FROM Sessions WHERE EndedAtUtc IS NULL)"; ev.Parameters.AddWithValue("$id", id); await ev.ExecuteNonQueryAsync(token); }
            await transaction.CommitAsync(token);
            return (await GetAsync(token)).Items.Single(x => x.Id == id);
        }
        finally { gate.Release(); }
    }

    public async Task IncreaseAsync(string id, CancellationToken token)
    {
        await using var connection = await OpenAsync(token); await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Items SET Quantity=Quantity+1,UpdatedAtUtc=$now WHERE Id=$id AND ArchivedAtUtc IS NULL AND Purchased=0 AND Quantity<999";
        command.Parameters.AddWithValue("$id", id); command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O")); if (await command.ExecuteNonQueryAsync(token) == 0) throw NotFound();
    }

    public async Task ReactivateAsync(string id, CancellationToken token)
    {
        await SetPurchasedAsync(id, false, token);
    }

    public async Task DeleteAsync(string id, CancellationToken token)
    {
        await using var connection = await OpenAsync(token); await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Items WHERE Id=$id AND ArchivedAtUtc IS NULL"; command.Parameters.AddWithValue("$id", id); if (await command.ExecuteNonQueryAsync(token) == 0) throw NotFound();
    }

    public async Task<IReadOnlyList<ShoppingSuggestion>> SuggestionsAsync(string query, CancellationToken token)
    {
        var q = query.Trim(); if (q.Length == 0) return [];
        await using var connection = await OpenAsync(token); var result = new List<ShoppingSuggestion>();
        await using var command = connection.CreateCommand(); command.CommandText = "SELECT DisplayName,PurchaseCount,LastPurchasedAtUtc FROM ItemStats WHERE DisplayName LIKE $q COLLATE NOCASE ORDER BY PurchaseCount DESC,LastPurchasedAtUtc DESC LIMIT 8"; command.Parameters.AddWithValue("$q", $"%{q}%");
        await using var reader = await command.ExecuteReaderAsync(token); while (await reader.ReadAsync(token)) result.Add(new(reader.GetString(0), reader.GetInt32(1)>0 ? "historik" : "tidigare"));
        var basics = new[] { "Mjölk","Bröd","Smör","Ägg","Ost","Kaffe","Bananer","Äpplen","Potatis","Ris","Pasta","Köttfärs","Kyckling","Tomater","Gurka","Toalettpapper","Hushållspapper","Diskmedel","Tvättmedel" };
        foreach (var item in basics.Where(x => x.Contains(q, StringComparison.CurrentCultureIgnoreCase))) if (result.All(x => Normalize(x.Name)!=Normalize(item))) result.Add(new(item,"grundlista"));
        return result.Take(8).ToArray();
    }

    public async Task<IReadOnlyList<FrequentItem>> FrequentAsync(CancellationToken token)
    {
        await using var connection = await OpenAsync(token); var result = new List<FrequentItem>(); await using var command = connection.CreateCommand();
        command.CommandText = "SELECT DisplayName,PurchaseCount,LastPurchasedAtUtc FROM ItemStats WHERE PurchaseCount>0 ORDER BY PurchaseCount DESC,LastPurchasedAtUtc DESC,DisplayName LIMIT 8";
        await using var reader = await command.ExecuteReaderAsync(token); while(await reader.ReadAsync(token)) result.Add(new(reader.GetString(0),reader.GetInt32(1),reader.IsDBNull(2)?null:DateTimeOffset.Parse(reader.GetString(2), CultureInfo.InvariantCulture)));
        return result;
    }

    public async Task<FinishShoppingResult> FinishAsync(bool keepUnpurchased, CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            await using var connection = await OpenAsync(token); await using var transaction=(SqliteTransaction)await connection.BeginTransactionAsync(token); var now=DateTimeOffset.UtcNow;
            var sessionId = await ScalarAsync(connection,"SELECT Id FROM Sessions WHERE EndedAtUtc IS NULL ORDER BY StartedAtUtc DESC LIMIT 1",token,transaction) as string;
            if (sessionId is not null)
            {
                await using var stats=connection.CreateCommand(); stats.Transaction=transaction; stats.CommandText="""
                    INSERT INTO ItemStats(NormalizedName,DisplayName,AddedCount,PurchaseCount,LastPurchasedAtUtc,AveragePosition,ObservationCount)
                    SELECT e.NormalizedName,MAX(i.Name),0,COUNT(*),MAX(e.CheckedAtUtc),AVG(CAST(e.Position AS REAL)),COUNT(*) FROM CheckEvents e JOIN Items i ON i.Id=e.ItemId WHERE e.SessionId=$session GROUP BY e.NormalizedName
                    ON CONFLICT(NormalizedName) DO UPDATE SET PurchaseCount=PurchaseCount+excluded.PurchaseCount,LastPurchasedAtUtc=excluded.LastPurchasedAtUtc,
                    AveragePosition=((ItemStats.AveragePosition*ItemStats.ObservationCount)+(excluded.AveragePosition*excluded.ObservationCount*1.15))/(ItemStats.ObservationCount+(excluded.ObservationCount*1.15)),ObservationCount=ItemStats.ObservationCount+excluded.ObservationCount
                    """; stats.Parameters.AddWithValue("$session",sessionId); await stats.ExecuteNonQueryAsync(token);
                await using var end=connection.CreateCommand(); end.Transaction=transaction; end.CommandText="UPDATE Sessions SET EndedAtUtc=$now WHERE Id=$id"; end.Parameters.AddWithValue("$now",now.ToString("O")); end.Parameters.AddWithValue("$id",sessionId); await end.ExecuteNonQueryAsync(token);
            }
            await using var count=connection.CreateCommand(); count.Transaction=transaction; count.CommandText="SELECT COUNT(*) FROM Items WHERE Purchased=1 AND ArchivedAtUtc IS NULL"; var archived=Convert.ToInt32(await count.ExecuteScalarAsync(token), CultureInfo.InvariantCulture);
            await using var archive=connection.CreateCommand(); archive.Transaction=transaction; archive.CommandText="UPDATE Items SET ArchivedAtUtc=$now WHERE Purchased=1 AND ArchivedAtUtc IS NULL"; archive.Parameters.AddWithValue("$now",now.ToString("O")); await archive.ExecuteNonQueryAsync(token);
            if (!keepUnpurchased) { await using var remove=connection.CreateCommand(); remove.Transaction=transaction; remove.CommandText="DELETE FROM Items WHERE Purchased=0 AND ArchivedAtUtc IS NULL"; await remove.ExecuteNonQueryAsync(token); }
            await transaction.CommitAsync(token); return new(archived,(await GetAsync(token)).Items.Count);
        }
        finally { gate.Release(); }
    }

    private void Initialize()
    {
        using var connection = new SqliteConnection(connectionString); connection.Open(); using var command=connection.CreateCommand(); command.CommandText="""
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS SchemaInfo(Version INTEGER NOT NULL);
            INSERT INTO SchemaInfo(Version) SELECT 1 WHERE NOT EXISTS(SELECT 1 FROM SchemaInfo);
            CREATE TABLE IF NOT EXISTS Items(Id TEXT PRIMARY KEY,Name TEXT NOT NULL,NormalizedName TEXT NOT NULL,Quantity INTEGER NOT NULL CHECK(Quantity BETWEEN 1 AND 999),Purchased INTEGER NOT NULL CHECK(Purchased IN(0,1)),CreatedAtUtc TEXT NOT NULL,UpdatedAtUtc TEXT NOT NULL,ArchivedAtUtc TEXT NULL,SortOrdinal INTEGER NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS UX_Items_ActiveName ON Items(NormalizedName) WHERE ArchivedAtUtc IS NULL;
            CREATE TABLE IF NOT EXISTS Sessions(Id TEXT PRIMARY KEY,StartedAtUtc TEXT NOT NULL,EndedAtUtc TEXT NULL);
            CREATE TABLE IF NOT EXISTS CheckEvents(SessionId TEXT NOT NULL REFERENCES Sessions(Id),ItemId TEXT NOT NULL REFERENCES Items(Id),NormalizedName TEXT NOT NULL,Position INTEGER NOT NULL,CheckedAtUtc TEXT NOT NULL,PRIMARY KEY(SessionId,ItemId));
            CREATE TABLE IF NOT EXISTS ItemStats(NormalizedName TEXT PRIMARY KEY,DisplayName TEXT NOT NULL,AddedCount INTEGER NOT NULL DEFAULT 0,PurchaseCount INTEGER NOT NULL DEFAULT 0,LastPurchasedAtUtc TEXT NULL,AveragePosition REAL NOT NULL DEFAULT 0,ObservationCount INTEGER NOT NULL DEFAULT 0);
            """; command.ExecuteNonQuery(); using var version=connection.CreateCommand(); version.CommandText="SELECT Version FROM SchemaInfo LIMIT 1"; if(Convert.ToInt32(version.ExecuteScalar(), CultureInfo.InvariantCulture)!=SchemaVersion) throw new InvalidDataException("Unsupported shopping list schema.");
    }
    private async Task<SqliteConnection> OpenAsync(CancellationToken token) { if(!IsAvailable) throw new ShoppingListUnavailableException(); var c=new SqliteConnection(connectionString); try { await c.OpenAsync(token); return c; } catch { await c.DisposeAsync(); throw new ShoppingListUnavailableException(); } }
    private static async Task<object?> ScalarAsync(SqliteConnection c,string sql,CancellationToken token,SqliteTransaction? t=null){await using var cmd=c.CreateCommand();cmd.Transaction=t;cmd.CommandText=sql;return await cmd.ExecuteScalarAsync(token);}
    private static async Task<string> EnsureSessionAsync(SqliteConnection c,SqliteTransaction t,CancellationToken token){var id=await ScalarAsync(c,"SELECT Id FROM Sessions WHERE EndedAtUtc IS NULL ORDER BY StartedAtUtc DESC LIMIT 1",token,t) as string;if(id is not null)return id;id=Guid.NewGuid().ToString("N");await using var cmd=c.CreateCommand();cmd.Transaction=t;cmd.CommandText="INSERT INTO Sessions VALUES($id,$now,NULL)";cmd.Parameters.AddWithValue("$id",id);cmd.Parameters.AddWithValue("$now",DateTimeOffset.UtcNow.ToString("O"));await cmd.ExecuteNonQueryAsync(token);return id;}
    private static async Task UpsertAddedStatAsync(SqliteConnection c,string n,string display,CancellationToken token){await using var cmd=c.CreateCommand();cmd.CommandText="INSERT INTO ItemStats(NormalizedName,DisplayName,AddedCount) VALUES($n,$d,1) ON CONFLICT(NormalizedName) DO UPDATE SET DisplayName=$d,AddedCount=AddedCount+1";cmd.Parameters.AddWithValue("$n",n);cmd.Parameters.AddWithValue("$d",display);await cmd.ExecuteNonQueryAsync(token);}
    private static void AddItemParameters(SqliteCommand c,ShoppingItem i){c.Parameters.AddWithValue("$id",i.Id);c.Parameters.AddWithValue("$name",i.Name);c.Parameters.AddWithValue("$normalized",i.NormalizedName);c.Parameters.AddWithValue("$quantity",i.Quantity);c.Parameters.AddWithValue("$now",i.CreatedAtUtc.ToString("O"));c.Parameters.AddWithValue("$ordinal",i.SortOrdinal);}
    private static ShoppingItem ReadItem(SqliteDataReader r)=>new(r.GetString(0),r.GetString(1),r.GetString(2),r.GetInt32(3),r.GetInt32(4)==1,DateTimeOffset.Parse(r.GetString(5),CultureInfo.InvariantCulture),DateTimeOffset.Parse(r.GetString(6),CultureInfo.InvariantCulture),r.GetInt32(7));
    private static void ValidateQuantity(int q){if(q is <1 or >999)throw Invalid("Antal måste vara mellan 1 och 999.");}
    private static ShoppingListException Invalid(string message)=>new(ShoppingListErrorCodes.InvalidRequest,message,400);
    private static ShoppingListException NotFound()=>new(ShoppingListErrorCodes.NotFound,"Varan hittades inte.",404);
    [GeneratedRegex(@"\s+")] private static partial Regex Spaces();
    public void Dispose() => gate.Dispose();
}
