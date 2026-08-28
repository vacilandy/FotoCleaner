using System.IO;
using Microsoft.Data.Sqlite;

namespace FotoCleaner.Services;

public sealed class HashDatabase
{
    private readonly string connectionString;
    public HashDatabase()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FotoCleaner");
        Directory.CreateDirectory(folder);
        connectionString = new SqliteConnectionStringBuilder { DataSource = Path.Combine(folder, "cache.db") }.ToString();
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE IF NOT EXISTS media_hashes (path TEXT PRIMARY KEY, size INTEGER NOT NULL, modified INTEGER NOT NULL, hash INTEGER NOT NULL, duration REAL NULL, width INTEGER NOT NULL DEFAULT 0, height INTEGER NOT NULL DEFAULT 0)";
        command.ExecuteNonQuery();
        using var migration = connection.CreateCommand();
        migration.CommandText = "ALTER TABLE media_hashes ADD COLUMN width INTEGER NOT NULL DEFAULT 0";
        try { migration.ExecuteNonQuery(); } catch (SqliteException) { }
        migration.CommandText = "ALTER TABLE media_hashes ADD COLUMN height INTEGER NOT NULL DEFAULT 0";
        try { migration.ExecuteNonQuery(); } catch (SqliteException) { }
    }
    public async Task<HashResult?> FindAsync(string path, FileInfo info, CancellationToken token)
    {
        await using var connection = Open(); await using var command = connection.CreateCommand();
        command.CommandText = "SELECT hash, duration, width, height FROM media_hashes WHERE size=$size AND modified=$modified ORDER BY CASE WHEN path=$path THEN 0 ELSE 1 END LIMIT 1";
        command.Parameters.AddWithValue("$path", path); command.Parameters.AddWithValue("$size", info.Length); command.Parameters.AddWithValue("$modified", info.LastWriteTimeUtc.Ticks);
        await using var reader = await command.ExecuteReaderAsync(token);
        if (!await reader.ReadAsync(token)) return null;
        return new HashResult((ulong)reader.GetInt64(0), reader.IsDBNull(1) ? null : TimeSpan.FromSeconds(reader.GetDouble(1)), reader.GetInt32(2), reader.GetInt32(3));
    }
    public async Task SaveManyAsync(IEnumerable<(string Path, FileInfo Info, HashResult Result)> entries, CancellationToken token)
    {
        await using var connection = Open();
        using var transaction = connection.BeginTransaction();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO media_hashes(path,size,modified,hash,duration,width,height) VALUES($path,$size,$modified,$hash,$duration,$width,$height) ON CONFLICT(path) DO UPDATE SET size=$size,modified=$modified,hash=$hash,duration=$duration,width=$width,height=$height";
        var pathParameter = command.Parameters.Add("$path", SqliteType.Text);
        var sizeParameter = command.Parameters.Add("$size", SqliteType.Integer);
        var modifiedParameter = command.Parameters.Add("$modified", SqliteType.Integer);
        var hashParameter = command.Parameters.Add("$hash", SqliteType.Integer);
        var durationParameter = command.Parameters.Add("$duration", SqliteType.Real);
        var widthParameter = command.Parameters.Add("$width", SqliteType.Integer);
        var heightParameter = command.Parameters.Add("$height", SqliteType.Integer);
        foreach (var entry in entries)
        {
            token.ThrowIfCancellationRequested();
            pathParameter.Value = entry.Path;
            sizeParameter.Value = entry.Info.Length;
            modifiedParameter.Value = entry.Info.LastWriteTimeUtc.Ticks;
            hashParameter.Value = unchecked((long)entry.Result.Hash);
            durationParameter.Value = entry.Result.Duration?.TotalSeconds ?? (object)DBNull.Value;
            widthParameter.Value = entry.Result.Width;
            heightParameter.Value = entry.Result.Height;
            await command.ExecuteNonQueryAsync(token);
        }
        await transaction.CommitAsync(token);
    }
    private SqliteConnection Open() { var connection = new SqliteConnection(connectionString); connection.Open(); return connection; }
}
