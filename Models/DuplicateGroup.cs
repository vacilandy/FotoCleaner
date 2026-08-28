namespace FotoCleaner.Models;

public sealed class DuplicateGroup
{
    public required string Label { get; init; }
    public required IReadOnlyList<SelectableMediaFile> Items { get; init; }
}
