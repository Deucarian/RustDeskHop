namespace RustDeskHop.Tests;

internal sealed class TestDirectory : IDisposable
{
    internal string Root { get; } = Path.Combine(Path.GetTempPath(), $"RustDeskHop-Tests-{Guid.NewGuid():N}");

    internal TestDirectory() => Directory.CreateDirectory(Root);

    internal string FilePath(string name) => Path.Combine(Root, name);

    public void Dispose()
    {
        var fullPath = Path.GetFullPath(Root);
        var tempRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath())) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase)
            || !Path.GetFileName(fullPath).StartsWith("RustDeskHop-Tests-", StringComparison.Ordinal))
            throw new InvalidOperationException("Refusing to clean a directory outside this test's temporary root.");
        Directory.Delete(fullPath, true);
    }
}
