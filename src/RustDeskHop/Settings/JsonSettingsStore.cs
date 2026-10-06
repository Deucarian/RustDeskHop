using RustDeskHop.Models;

namespace RustDeskHop.Settings
{
    internal sealed class JsonSettingsStore : ISettingsStore
    {
        #region Methods
        public AppSettings Load(out string? warning) => ConfigStore.Load(out warning);
        public void Save(AppSettings settings) => ConfigStore.Save(settings);
        #endregion
    }
}