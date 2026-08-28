using System.Collections.Concurrent;
using System.IO;
using FotoCleaner.Models;

namespace FotoCleaner.Services;

public sealed class MediaScanner(IHashService hasher, HashDatabase database)
{
    public async Task<IReadOnlyList<MediaFile>> ScanAsync(string root, IProgress<string>? progress, CancellationToken token)
    {
        var fullRoot = Path.GetFullPath(root);
        if (!Directory.Exists(fullRoot)) throw new DirectoryNotFoundException(fullRoot);
        var duplicateFolder = Path.Combine(fullRoot, "Duplicadas") + Path.DirectorySeparatorChar;
        var paths = Directory.EnumerateFiles(fullRoot, "*.*", SearchOption.AllDirectories)
            .Where(path => !Path.GetFullPath(path).StartsWith(duplicateFolder, StringComparison.OrdinalIgnoreCase))
            .Where(PerceptualHashService.IsSupported).ToArray();
        var results = new ConcurrentBag<MediaFile>();
        var completed = 0;
        var options = new ParallelOptions
        {
            // OpenCV no necesita tantos hilos para este trabajo y SQLite puede bloquearse con demasiadas escrituras.
            MaxDegreeOfParallelism = Math.Min(4, Math.Max(1, Environment.ProcessorCount / 2)),
            CancellationToken = token
        };
        var pendingCache = new ConcurrentBag<(string Path, FileInfo Info, HashResult Result)>();
        await Parallel.ForEachAsync(paths, options, async (path, ct) =>
        {
            try
            {
                progress?.Report($"Procesando {Path.GetFileName(path)} ({completed} de {paths.Length})");
                var info = new FileInfo(path);
                var cached = await database.FindAsync(path, info, ct);
                var hash = cached ?? await hasher.ComputeAsync(path, ct);
                if (cached is null) pendingCache.Add((path, info, hash));
                results.Add(new MediaFile(path, info.Name, info.Length, hash.Duration, hash.Hash));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                progress?.Report($"Omitido {Path.GetFileName(path)}: {ex.Message}");
            }
            finally
            {
                var finished = Interlocked.Increment(ref completed);
                progress?.Report($"Analizados {finished} de {paths.Length}");
            }
        });
        if (!pendingCache.IsEmpty)
        {
            progress?.Report($"Guardando {pendingCache.Count} hashes en caché...");
            await database.SaveManyAsync(pendingCache, token);
        }
        return results.ToArray();
    }
}
