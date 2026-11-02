using RustDeskHop.Models;

namespace RustDeskHop.UI
{
    // A management-window draft, separate from saved settings. Navigation never commits it.
    internal sealed class NetworkEditorDraft(ServerProfile profile, IEnumerable<TargetDefinition> targets)
    {
        #region Properties and Indexers
        internal ServerProfile Profile { get; } = profile;
        internal NetworkComputerDrafts Computers { get; } = new NetworkComputerDrafts(profile.Id, targets);
        internal int CurrentRow { get; set; } = -1;
        internal int CurrentColumn { get; set; }
        internal int FirstVisibleRow { get; set; }
        internal int HorizontalOffset { get; set; }
        internal string Feedback { get; set; } = "";
        #endregion
    }
}