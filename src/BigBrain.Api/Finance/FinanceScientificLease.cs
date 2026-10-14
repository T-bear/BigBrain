using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Finance;

// Existing science entry points share a durable exclusion with explicit recurring registration.
// A crashed legacy owner leaves a blocker; startup does not manufacture a release or a new grant.
internal sealed class FinanceScientificLease : IDisposable
{
    // Legacy engines are synchronous; nested calls may share this lease only on its owning thread.
    [ThreadStatic] private static string? _current;
    private readonly string _connection;
    private readonly string? _id;
    private FinanceScientificLease(string connection, string? id) { _connection = connection; _id = id; }
    internal static FinanceScientificLease Enter(string connection)
    {
        if (_current == connection) return new(connection, null);
        using var c = new SqliteConnection(connection); c.Open();
        using var tx = c.BeginTransaction(deferred: false);
        using var check = c.CreateCommand(); check.Transaction = tx;
        check.CommandText = "SELECT COUNT(*) FROM recurring_research_program";
        if (Convert.ToInt32(check.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) != 0)
            throw new InvalidOperationException("Explicit recurring authority excludes legacy scientific starts.");
        var id = Guid.NewGuid().ToString("N");
        using var insert = c.CreateCommand(); insert.Transaction = tx;
        insert.CommandText = "INSERT INTO finance_scientific_lease(owner) VALUES($id)";
        insert.Parameters.AddWithValue("$id", id); insert.ExecuteNonQuery();
        tx.Commit();
        _current = connection; return new(connection, id);
    }
    internal static void RequireLegacyAllowed(SqliteConnection c)
    {
        using var cmd = c.CreateCommand(); cmd.CommandText = "SELECT COUNT(*) FROM recurring_research_program";
        if (Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) != 0)
            throw new InvalidOperationException("Recurring authority excludes legacy learning mutation.");
    }
    public void Dispose()
    {
        if (_id is null) return;
        using var c = new SqliteConnection(_connection); c.Open(); using var cmd = c.CreateCommand();
        cmd.CommandText = "DELETE FROM finance_scientific_lease WHERE owner=$id";
        cmd.Parameters.AddWithValue("$id", _id); cmd.ExecuteNonQuery(); _current = null;
    }
}
