namespace RustDeskHop.Tests
{
    internal sealed class TestDirectory : IDisposable
    {
        #region Constructors and Destructors
        internal TestDirectory() => Directory.CreateDirectory(Root);
        #endregion

        #region Properties and Indexers
        internal string Root { get; } = Path.Combine(Path.GetTempPath(), $"RustDeskHop-Tests-{Guid.NewGuid():N}");
        #endregion

        #region Methods
        public void Dispose()
        {
            string fullPath = Path.GetFullPath(Root);
            string tempRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()))
                              + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase)
                || !Path.GetFileName(fullPath).StartsWith("RustDeskHop-Tests-", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Refusing to clean a directory outside this test's temporary root."
                                                   );
            }
            Directory.Delete(fullPath, true);
        }

        internal string FilePath(string name) => Path.Combine(Root, name);
        #endregion
    }
}