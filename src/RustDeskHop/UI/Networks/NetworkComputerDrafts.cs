using System.ComponentModel;
using RustDeskHop.Models;
using RustDeskHop.Connections;

namespace RustDeskHop.UI
{
    // Owns network-scoped edits. No saved object changes until every draft validates.
    internal sealed class NetworkComputerDrafts
    {
        #region Constants and Fields
        private readonly List<TargetDefinition> _original;
        private readonly string _profileId;
        #endregion

        #region Constructors and Destructors
        internal NetworkComputerDrafts(string profileId, IEnumerable<TargetDefinition> targets)
        {
            _profileId = profileId;
            _original = targets.Where(t => t.ProfileId == profileId).ToList();
            Items = new BindingList<ComputerDraft>(_original.Select(t => new ComputerDraft(t)).ToList());
        }
        #endregion

        #region Properties and Indexers
        internal BindingList<ComputerDraft> Items { get; }
        #endregion

        #region Methods
        internal ComputerDraft AddBlank()
        {
            ComputerDraft draft = new ComputerDraft(new TargetDefinition { Name = "", ProfileId = _profileId }, true);
            Items.Add(draft);
            return draft;
        }

        internal bool TryAdd(TargetDefinition target, out string? error)
        {
            if (target.ProfileId != _profileId
                || string.IsNullOrWhiteSpace(target.Name)
                || ComputerAddress.Validate(target.RustDeskId) is not null)
            {
                error = "A name and RustDesk ID for the selected network are required.";
                return false;
            }
            if (Items.Any(d => string.Equals(ComputerAddress.Normalize(d.RustDeskId),
                                             ComputerAddress.Normalize(target.RustDeskId),
                                             StringComparison.OrdinalIgnoreCase
                                            )
                         ))
            {
                error = "This RustDesk ID is already saved in this network.";
                return false;
            }
            TargetDefinition copy = new TargetDefinition
            {
                Name = target.Name.Trim(), RustDeskId = ComputerAddress.Normalize(target.RustDeskId),
                ProfileId = _profileId
            };
            Items.Add(new ComputerDraft(copy, true));
            error = null;
            return true;
        }

        internal bool TrySave(List<TargetDefinition> targets, out string? error)
        {
            error = Items.Select(Validate).FirstOrDefault(message => message is not null);
            if (error is not null)
                return false;

            HashSet<TargetDefinition> retained = Items.Select(d => d.Target).ToHashSet();
            targets.RemoveAll(t => _original.Contains(t) && !retained.Contains(t));
            foreach (ComputerDraft draft in Items)
            {
                draft.Target.Name = draft.Name.Trim();
                if (draft.IsNew)
                    draft.Target.RustDeskId = ComputerAddress.Normalize(draft.RustDeskId);
                if (!targets.Contains(draft.Target))
                    targets.Add(draft.Target);
                draft.AcceptSaved();
            }
            _original.Clear();
            _original.AddRange(Items.Select(d => d.Target));
            error = null;
            return true;
        }

        internal string? Validate(ComputerDraft draft)
        {
            if (string.IsNullOrWhiteSpace(draft.Name))
                return "Every computer needs a name. No changes were saved.";

            string? error = ComputerAddress.Validate(draft.RustDeskId);
            if (error is not null)
                return error;
            if (draft.IsNew
                && Items.Any(other => other != draft
                                      && string.Equals(ComputerAddress.Normalize(other.RustDeskId),
                                                       ComputerAddress.Normalize(draft.RustDeskId),
                                                       StringComparison.OrdinalIgnoreCase
                                                      )
                            ))
                return "This RustDesk ID is already saved in this network.";

            return null;
        }

        internal TargetDefinition Snapshot(ComputerDraft draft) => new TargetDefinition
        {
            Name = draft.Name.Trim(), RustDeskId = ComputerAddress.Normalize(draft.RustDeskId), ProfileId = _profileId
        };
        #endregion
    }

    internal sealed class ComputerDraft(TargetDefinition target, bool isNew = false)
    {
        #region Properties and Indexers
        internal TargetDefinition Target { get; } = target;
        public string Name { get; set; } = target.Name;
        internal bool IsNew { get; private set; } = isNew;
        public string RustDeskId { get; set; } = target.RustDeskId;
        #endregion

        #region Methods
        internal void AcceptSaved()
        {
            Name = Target.Name;
            RustDeskId = Target.RustDeskId;
            IsNew = false;
        }
        #endregion
    }
}