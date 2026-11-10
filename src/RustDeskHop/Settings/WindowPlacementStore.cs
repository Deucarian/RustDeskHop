using System.Text.Json;

namespace RustDeskHop.Settings
{
    internal sealed record WindowPlacement(double ClientWidth, double ClientHeight, bool Maximized);

    internal interface IWindowPlacementStore
    {
        WindowPlacement? Load();
        void Save(WindowPlacement placement);
    }

    internal sealed class WindowPlacementStore(string path) : IWindowPlacementStore
    {
        #region Methods
        public WindowPlacement? Load()
        {
            try
            {
                WindowPlacement? placement = JsonSerializer.Deserialize<WindowPlacement>(File.ReadAllText(path));
                return placement is not null
                       && double.IsFinite(placement.ClientWidth) && placement.ClientWidth > 0
                       && double.IsFinite(placement.ClientHeight) && placement.ClientHeight > 0
                    ? placement : null;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
            {
                return null;
            }
        }

        public void Save(WindowPlacement placement)
        {
            string temporary = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
                File.WriteAllText(temporary, JsonSerializer.Serialize(placement));
                File.Move(temporary, path, true);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                // Window preferences are best effort and never block closing or affect computer settings.
            }
            finally
            {
                try { File.Delete(temporary); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
        #endregion
    }
}
