using System.IO;

namespace FotoCleaner.Services;

public sealed class FileRelocationService
{
    public Task<int> MoveAsync(IEnumerable<string> files, string root, CancellationToken token)
    {
        var fullRoot = Path.GetFullPath(root);
        var destination = Path.Combine(fullRoot, "Duplicadas");
        Directory.CreateDirectory(destination);
        var count = 0;
        foreach (var source in files)
        {
            token.ThrowIfCancellationRequested();
            var fullSource = Path.GetFullPath(source);
            var isInsideRoot = fullSource.Equals(fullRoot, StringComparison.OrdinalIgnoreCase)
                || fullSource.StartsWith(fullRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
            if (!isInsideRoot || fullSource.StartsWith(destination.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) continue;
            var target = Path.Combine(destination, Path.GetFileName(fullSource));
            var stem = Path.GetFileNameWithoutExtension(target); var extension = Path.GetExtension(target); var suffix = 1;
            while (File.Exists(target)) target = Path.Combine(destination, $"{stem}_{suffix++}{extension}");
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    File.Move(fullSource, target);
                    count++;
                    break;
                }
                catch (IOException) when (attempt < 4)
                {
                    Thread.Sleep(150 * (attempt + 1));
                }
            }
        }
        return Task.FromResult(count);
    }
}
