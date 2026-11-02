using RustDeskHop.Models;

namespace RustDeskHop.Settings
{
    internal interface ISettingsStore
    {
        AppSettings Load(out string? warning);
        void Save(AppSettings settings);
    }
}