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
        command.CommandText = "CREATE TABLE IF NOT EXISTS media_hashes (path TEXT PRIMARY KEY, size INTEGER NOT NULL, modified INTEGER NOT NULL, hash INTEGER NOT NULL, duration REAL NULL)";
        command.ExecuteNonQuery();
    }
    public async Task<HashResult?> FindAsync(string path, FileInfo info, CancellationToken token)
    {
        await using var connection = Open(); await using var command = connection.CreateCommand();
        command.CommandText = "SELECT hash, duration FROM media_hashes WHERE path=$path AND size=$size AND modified=$modified";
        command.Parameters.AddWithValue("$path", path); command.Parameters.AddWithValue("$size", info.Length); command.Parameters.AddWithValue("$modified", info.LastWriteTimeUtc.Ticks);
        await using var reader = await command.ExecuteReaderAsync(token);
        if (!await reader.ReadAsync(token)) return null;
        return new HashResult((ulong)reader.GetInt64(0), reader.IsDBNull(1) ? null : TimeSpan.FromSeconds(reader.GetDouble(1)));
    }
    public async Task SaveAsync(string path, FileInfo info, HashResult result, CancellationToken token)
    {
        await using var connection = Open(); await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO media_hashes(path,size,modified,hash,duration) VALUES($path,$size,$modified,$hash,$duration) ON CONFLICT(path) DO UPDATE SET size=$size,modified=$modified,hash=$hash,duration=$duration";
        command.Parameters.AddWithValue("$path", path); command.Parameters.AddWithValue("$size", info.Length); command.Parameters.AddWithValue("$modified", info.LastWriteTimeUtc.Ticks); command.Parameters.AddWithValue("$hash", unchecked((long)result.Hash)); command.Parameters.AddWithValue("$duration", result.Duration?.TotalSeconds ?? (object)DBNull.Value);
        await command.ExecuteNonQueryAsync(token);
    }
    private SqliteConnection Open() { var connection = new SqliteConnection(connectionString); connection.Open(); return connection; }
}
