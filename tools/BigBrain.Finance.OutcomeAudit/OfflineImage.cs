using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

namespace BigBrain.Finance.OutcomeAudit;

// No executable, API integration, live reader or snapshot creation path. This internal
// prototype is exercised only by synthetic tests. A digest is integrity, NOT authority
// or proof of snapshot completeness/stability. The future operator boundary is blocked.
internal static class OfflineImage
{
    private static readonly string[] SidecarSuffixes = ["-wal", "-shm", "-journal"];

    internal static AuditReport Read(string path, string expectedSha256, DateTimeOffset snapshotUtc)
    {
        if (snapshotUtc.Offset != TimeSpan.Zero || snapshotUtc.Year is < 2000 or > 2101) return AuditReport.Stopped("image.snapshot-clock");
        try
        {
            path = Path.GetFullPath(path);
            for (FileSystemInfo? entry = new FileInfo(path); entry is not null;
                entry = entry is FileInfo file ? file.Directory : ((DirectoryInfo)entry).Parent)
                if (entry.LinkTarget is not null) return AuditReport.Stopped("image.link");
            if (Sidecars(path)) return AuditReport.Stopped("image.sidecar");
            using var image = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (image.Length is < 100 or > 268435456) return AuditReport.Stopped("image.size");
            var header = new byte[100];
            image.ReadExactly(header);
            if (!header.AsSpan(0, 16).SequenceEqual("SQLite format 3\0"u8) || header[18] != 1 || header[19] != 1)
                return AuditReport.Stopped("image.not-complete-rollback-image");
            image.Position = 0;
            var digest = Convert.ToHexString(SHA256.HashData(image));
            if (!string.Equals(digest, expectedSha256, StringComparison.OrdinalIgnoreCase))
                return AuditReport.Stopped("image.digest");
            AuditReport report;
            using (var connection = OpenImmutable(path))
            using (var transaction = connection.BeginTransaction(deferred: true))
            {
                report = LedgerAudit.Read(connection, transaction, snapshotUtc);
                // Disposal rolls back a read transaction; no checkpoint/cleanup/DDL.
            }
            image.Position = 0;
            if (Sidecars(path) || !digest.Equals(Convert.ToHexString(SHA256.HashData(image)), StringComparison.Ordinal))
                return AuditReport.Stopped("image.changed");
            return report;
        }
        catch (SqliteException) { return AuditReport.Stopped("image.sqlite"); }
        catch (IOException) { return AuditReport.Stopped("image.io"); }
        catch (UnauthorizedAccessException) { return AuditReport.Stopped("image.access"); }
        catch (ArgumentException) { return AuditReport.Stopped("image.argument"); }
    }

    private static bool Sidecars(string path) => SidecarSuffixes
        .Any(suffix => File.Exists(path + suffix) || Directory.Exists(path + suffix));

    internal static SqliteConnection OpenImmutable(string path)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = new Uri(Path.GetFullPath(path)).AbsoluteUri + "?immutable=1",
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
            DefaultTimeout = 1
        }.ToString());
        try
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA query_only=ON; PRAGMA trusted_schema=OFF; PRAGMA temp_store=MEMORY;";
            command.ExecuteNonQuery();
            return connection;
        }
        catch { connection.Dispose(); throw; }
    }
}
