namespace RustDeskHop.Models
{
    internal sealed class AppSettings
    {
        #region Constants and Fields
        private int _uiScalePercent = UiScalePreference.DEFAULT;
        #endregion

        #region Properties and Indexers
        public int UiScalePercent
        {
            get => _uiScalePercent;
            set => _uiScalePercent = UiScalePreference.Normalize(value);
        }

        public List<ServerProfile> Profiles { get; set; } = [];
        public List<TargetDefinition> Targets { get; set; } = [];
        #endregion
    }
}