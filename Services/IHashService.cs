namespace FotoCleaner.Services;

public sealed record HashResult(ulong Hash, TimeSpan? Duration);

public interface IHashService
{
    Task<HashResult> ComputeAsync(string path, CancellationToken cancellationToken);
    static int HammingDistance(ulong left, ulong right) => System.Numerics.BitOperations.PopCount(left ^ right);
}
