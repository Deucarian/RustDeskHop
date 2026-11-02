using RustDeskHop.Models;

namespace RustDeskHop.UI
{
    internal sealed partial class ProfilesForm
    {
        #region Methods
        private void LoadSelected()
        {
            // Native reselect and binding notifications are not requests to discard the current editor.
            if (IsApplyingUiScale || _rebindingProfiles)
                return;

            _selectedIndex = _profileList.SelectedIndex;
            ServerProfile? profile = _profileList.SelectedItem as ServerProfile;
            if (_activeDraft?.Profile.Id == profile?.Id)
                return;

            CaptureNetworkFields();
            _networkTransition.Begin();
            _activeDraft = null;
            if (profile is not null)
            {
                if (!_drafts.TryGetValue(profile.Id, out NetworkEditorDraft? draft))
                {
                    draft = new NetworkEditorDraft(Clone(profile), Targets);
                    _drafts.Add(profile.Id, draft);
                }
                _activeDraft = draft;
                RestoreNetworkFields(draft.Profile);
            }
            _computers.LoadNetwork(_activeDraft, Targets);
            _networkTransition.End();
        }

        private void CaptureNetworkFields()
        {
            if (_activeDraft is null)
                return;

            ServerProfile draft = _activeDraft.Profile;
            draft.Name = _nameBox.Text;
            draft.ServerAddress = _addressBox.Text;
            draft.PublicKey = _keyBox.Text;
            draft.RequiresPrivateNetwork = _privateNetworkBox.Checked;
            draft.ProbeHost = _probeHostBox.Text;
            draft.ProbePort = (int)_probePortBox.Value;
        }

        private void RestoreNetworkFields(ServerProfile profile)
        {
            _nameBox.Text = profile.Name;
            _addressBox.Text = profile.ServerAddress;
            _keyBox.Text = profile.PublicKey;
            _privateNetworkBox.Checked = profile.RequiresPrivateNetwork;
            _probeHostBox.Text = profile.ProbeHost;
            _probePortBox.Value = Math.Clamp(profile.ProbePort, 1, 65535);
        }

        private void RebindProfiles(int index)
        {
            _networkListTransition.Begin();
            _rebindingProfiles = true;
            _profileList.BeginUpdate();
            try
            {
                _profileList.DataSource = null;
                _profileList.DisplayMember = nameof(ServerProfile.Name);
                _profileList.DataSource = _profiles;
                _profileList.SelectedIndex = index >= 0 && index < _profiles.Count ? index : -1;
            }
            finally
            {
                _profileList.EndUpdate();
                _rebindingProfiles = false;
            }
            LoadSelected();
            _networkListTransition.End(true);
        }
        #endregion
    }
}