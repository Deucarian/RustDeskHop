namespace RustDeskHop.Models
{
    internal sealed class AppSettings
    {
        #region Properties and Indexers
        public List<ServerProfile> Profiles { get; set; } = [];
        public List<TargetDefinition> Targets { get; set; } = [];
        #endregion
    }
}