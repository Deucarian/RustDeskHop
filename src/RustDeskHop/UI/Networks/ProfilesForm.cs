using RustDeskHop.Models;
using RustDeskHop.UI.Controls;
using RustDeskHop.UI.Theme;
using RustDeskHop.Connections;

namespace RustDeskHop.UI
{
    internal sealed class ProfilesForm : BrandedForm
    {
        #region Constants and Fields
        private readonly HoverListBox _profileList = new HoverListBox();
        private readonly TextBox _nameBox = new TextBox();
        private readonly TextBox _addressBox = new TextBox();
        private readonly TextBox _keyBox = new TextBox();
        private readonly CheckBox _privateNetworkBox = new AnimatedCheckBox()
        {
            Text = "Requires Tailscale/private network",
            AutoSize = true
        };
        private readonly TextBox _probeHostBox = new TextBox();
        private readonly NumericUpDown _probePortBox = new NumericUpDown()
        {
            Minimum = 1,
            Maximum = 65535,
            Value = 21116
        };
        private readonly List<ServerProfile> _profiles;
        private readonly NetworkComputersEditor _computers;
        private readonly Func<IWin32Window, TargetDefinition, ServerProfile, Task<ConnectionOutcome>>? _testConnection;
        private bool _testing;
        private int _selectedIndex = -1;
        private SectionTabs _sections = null!;
        private readonly ContentTransition _networkTransition;
        private readonly ContentTransition _networkListTransition;
        #endregion

        #region Constructors and Destructors
        public ProfilesForm(IEnumerable<ServerProfile> source,
                            IEnumerable<TargetDefinition>? targets = null,
                            Func<IWin32Window, TargetDefinition, ServerProfile, Task<ConnectionOutcome>>?
                                testConnection = null)
        {
            _testConnection = testConnection;
            _networkListTransition = new ContentTransition(_profileList);
            _computers = new NetworkComputersEditor(testConnection is null ? null : TestComputerAsync);
            _profiles = source.Select(Clone).ToList();
            Targets = (targets ?? [])
                .Select(t => new TargetDefinition { Name = t.Name, RustDeskId = t.RustDeskId, ProfileId = t.ProfileId, }
                       )
                .ToList();
            Text = "Manage computers & networks";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(780, 330);
            ClientSize = new Size(900, 440);
            WindowContent.BackColor = Color.White;
            SurfacePanel surface = new SurfacePanel
            {
                Padding = Padding.Empty,
                Outlined = false
            };
            TableLayoutPanel split = new TableLayoutPanel
            {
                Name = "NetworkPanes",
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = AppTheme.line
            };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 196));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            split.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Panel sidebar = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 1, 0),
                BackColor = AppTheme.canvas,
                Padding = new Padding(8, UiMetrics.PAGE_INSET, 8, UiMetrics.PAGE_INSET)
            };
            Panel detailArea = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                BackColor = Color.White,
                Padding = new Padding(UiMetrics.PAGE_INSET)
            };
            split.Controls.Add(sidebar, 0, 0);
            split.Controls.Add(detailArea, 1, 0);
            _profileList.Dock = DockStyle.Fill;
            _profileList.Name = "Networks";
            _profileList.AccessibleName = "Saved networks";
            _profileList.BorderStyle = BorderStyle.None;
            _profileList.BackColor = AppTheme.canvas;
            _profileList.IntegralHeight = false;
            _profileList.DrawMode = DrawMode.OwnerDrawFixed;
            _profileList.ItemHeight = UiMetrics.ROW_HEIGHT;
            _profileList.DrawItem += (_, e) =>
            {
                if (e.Index < 0)
                    return;

                bool selected = (e.State & DrawItemState.Selected) != 0;
                using SolidBrush background = new SolidBrush(UiMotion.Blend(AppTheme.canvas,
                                                                           AppTheme.selection,
                                                                           _profileList.HoverAmount(e.Index)
                                                                          )
                                                            );
                e.Graphics.FillRectangle(background, e.Bounds);
                if (selected)
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using System.Drawing.Drawing2D.GraphicsPath shape = AppTheme.Round(e.Bounds, 8 * UiScale.Factor(this));
                    using SolidBrush selection = new SolidBrush(Color.FromArgb(230, 239, 253));
                    e.Graphics.FillPath(selection, shape);
                }
                if (selected)
                {
                    using SolidBrush accent = new SolidBrush(AppTheme.blue);
                    e.Graphics.FillRectangle(accent,
                                             e.Bounds.Left,
                                             e.Bounds.Top + 10 * UiScale.Factor(this),
                                             3 * UiScale.Factor(this),
                                             e.Bounds.Height - 20 * UiScale.Factor(this)
                                            );
                }

                Rectangle textBounds =
                    Rectangle.Inflate(e.Bounds, -(int)(12 * UiScale.Factor(this)), -(int)(6 * UiScale.Factor(this)));
                TextRenderer.DrawText(e.Graphics,
                                      _profileList.GetItemText(_profileList.Items[e.Index]),
                                      _profileList.Font,
                                      textBounds,
                                      AppTheme.ink,
                                      TextFormatFlags.WordBreak
                                      | TextFormatFlags.VerticalCenter
                                      | TextFormatFlags.NoPrefix
                                     );
                if ((e.State & DrawItemState.Focus) != 0)
                    e.DrawFocusRectangle();
            };
            _profileList.DisplayMember = nameof(ServerProfile.Name);
            _profileList.SelectedIndexChanged += (_, _) => LoadSelected();
            sidebar.Controls.Add(_profileList);
            TableLayoutPanel editor = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = Padding.Empty,
                ColumnCount = 2,
                RowCount = 7,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
            };
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int row = 0; row < 6; row++)
            {
                editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            EditorLayout.AddRow(editor, 0, "Profile name", _nameBox);
            EditorLayout.AddRow(editor, 1, "Server address", _addressBox);
            EditorLayout.AddRow(editor, 2, "Public key", _keyBox);
            EditorLayout.AddRow(editor, 3, "Probe host", _probeHostBox);
            EditorLayout.AddRow(editor, 4, "Probe port", _probePortBox);
            _privateNetworkBox.Margin = new Padding(0, UiMetrics.GAP, 0, UiMetrics.GAP);
            editor.Controls.Add(_privateNetworkBox, 0, 5);
            editor.SetColumnSpan(_privateNetworkBox, 2);
            Label hint = new Label
            {
                Text =
                    "Use “public” for RustDesk’s public network. Private profiles use the ID server address and its public key.",
                AutoSize = true,
                Dock = DockStyle.Top,
                ForeColor = AppTheme.muted,
                Font = AppTheme.small,
                Margin = new Padding(0, UiMetrics.GAP, 0, UiMetrics.GAP),
            };
            editor.Controls.Add(hint, 0, 6);
            editor.SetColumnSpan(hint, 2);

            void UpdateHintWidth()
            {
                int availableWidth = Math.Max(240,
                                              editor.ClientSize.Width
                                              - editor.Padding.Horizontal
                                              - hint.Margin.Horizontal
                                             );
                hint.MaximumSize = new Size(availableWidth, 0);
            }

            editor.SizeChanged += (_, _) => UpdateHintWidth();
            FlowLayoutPanel buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = true,
                AutoSize = true,
                Margin = new Padding(0, UiMetrics.GAP, 0, 0),
            };
            ModernButton add = new ModernButton
            {
                Text = "New",
                Glyph = UiGlyph.PLUS,
                AutoSize = true,
                Quiet = true,
                Accent = true,
                AlignLeft = true
            };
            Panel newNetworkArea = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = UiMetrics.BUTTON_HEIGHT + UiMetrics.INSET
            };
            add.Text = "New network";
            add.Dock = DockStyle.Bottom;
            newNetworkArea.Controls.Add(add);
            sidebar.Controls.Add(newNetworkArea);
            ModernButton save = new ModernButton
            {
                Name = "SaveNetwork",
                Text = "Save network",
                Primary = true,
                AutoSize = true,
                Margin = Padding.Empty
            };
            save.Click += (_, _) => SaveSelected();
            buttons.Controls.Add(save);
            ModernButton remove = new ModernButton
            {
                Name = "DeleteNetwork",
                Text = "Delete network",
                Quiet = true,
                AutoSize = true
            };
            remove.Click += (_, _) => DeleteSelected();
            ModernButton close = new ModernButton
            {
                Text = "Close",
                MinimumSize = new Size(88, UiMetrics.BUTTON_HEIGHT),
                DialogResult = DialogResult.OK,
                AutoSize = true
            };
            buttons.Controls.Add(close);
            buttons.Controls.Add(remove);
            SectionTabs sections = new SectionTabs
            {
                Name = "NetworkSections",
                AccessibleName = "Network sections",
                Dock = DockStyle.Fill,
                Font = AppTheme.small,
                Margin = Padding.Empty
            };
            _sections = sections;
            _networkTransition = sections.Transition;
            Panel settingsPage = new Panel
            {
                Text = "Network settings",
                BackColor = Color.White,
                Padding = Padding.Empty
            };
            Panel computersPage = new Panel
            {
                Text = "Computers",
                BackColor = Color.White,
                Padding = Padding.Empty
            };
            settingsPage.Controls.Add(editor);
            _computers.Dock = DockStyle.Fill;
            computersPage.Controls.Add(_computers);
            sections.AddPage(settingsPage);
            sections.AddPage(computersPage);
            sections.SelectedIndex = 1;
            remove.Visible = false;
            sections.SelectedIndexChanged += (_, _) => remove.Visible = sections.SelectedIndex == 0;
            add.Click += (_, _) =>
            {
                sections.SelectedIndex = 0;
                NewProfile();
            };
            TableLayoutPanel detail = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = Padding.Empty,
                Margin = Padding.Empty,
            };
            detail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            detail.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            detail.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            detail.Controls.Add(sections, 0, 0);
            detail.Controls.Add(buttons, 0, 1);
            detailArea.Controls.Add(detail);
            surface.Controls.Add(split);
            WindowContent.Controls.Add(surface);
            WindowContent.Layout += (_, _) =>
            {
                float scale = UiScale.Factor(this);
                int width = Math.Min((int)(UiMetrics.CONTENT_WIDTH * scale),
                                     WindowContent.ClientSize.Width - WindowContent.Padding.Horizontal
                                    );
                int height = Math.Min((int)(500 * scale),
                                      WindowContent.ClientSize.Height - WindowContent.Padding.Vertical
                                     );
                surface.SetBounds((WindowContent.ClientSize.Width - width) / 2,
                                  0,
                                  width,
                                  height
                                 );
            };
            AcceptButton = save;
            CancelButton = close;
            FormClosing += (_, e) =>
            {
                if (_testing && e.CloseReason == CloseReason.UserClosing)
                    e.Cancel = true;
            };
            Shown += (_, _) =>
            {
                UpdateHintWidth();
                RebindProfiles(_profiles.Count > 0 ? 0 : -1);
            };
        }
        #endregion

        #region Properties and Indexers
        public List<ServerProfile> Profiles => _profiles;
        public List<TargetDefinition> Targets { get; }
        #endregion

        #region Methods
        private void LoadSelected()
        {
            // Font/item-height changes can recreate the native list handle and raise selection events.
            // That is layout, not a request to reload the network and discard its unsaved drafts.
            if (IsApplyingUiScale)
                return;

            _networkTransition.Begin();
            if (_profileList.SelectedIndex < 0 || _profileList.SelectedIndex >= _profiles.Count)
            {
                _selectedIndex = -1;
                _computers.LoadNetwork(null, Targets);
                _networkTransition.End();
                return;
            }

            _selectedIndex = _profileList.SelectedIndex;
            ServerProfile profile = _profiles[_selectedIndex];
            _nameBox.Text = profile.Name;
            _addressBox.Text = profile.ServerAddress;
            _keyBox.Text = profile.PublicKey;
            _privateNetworkBox.Checked = profile.RequiresPrivateNetwork;
            _probeHostBox.Text = profile.ProbeHost;
            _probePortBox.Value = Math.Clamp(profile.ProbePort, 1, 65535);
            _computers.LoadNetwork(profile, Targets);
            _networkTransition.End();
        }

        private void NewProfile()
        {
            ServerProfile profile = new ServerProfile
            {
                Name = "New network",
                ServerAddress = "",
                RequiresPrivateNetwork = true
            };
            _profiles.Add(profile);
            RebindProfiles(_profiles.Count - 1);
        }

        private void SaveSelected()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _profiles.Count)
                return;

            if (string.IsNullOrWhiteSpace(_nameBox.Text) || string.IsNullOrWhiteSpace(_addressBox.Text))
            {
                MessageBox.Show(this,
                                "Profile name and server address are required.",
                                "Incomplete",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning
                               );
                return;
            }

            if (!_computers.TrySave(out _))
                return;

            ServerProfile profile = _profiles[_selectedIndex];
            profile.Name = _nameBox.Text.Trim();
            profile.ServerAddress = _addressBox.Text.Trim();
            profile.PublicKey = _keyBox.Text.Trim();
            profile.RequiresPrivateNetwork = _privateNetworkBox.Checked;
            profile.ProbeHost = _probeHostBox.Text.Trim();
            profile.ProbePort = (int)_probePortBox.Value;
            RebindProfiles(_selectedIndex);
        }

        private async Task<ConnectionOutcome?> TestComputerAsync(TargetDefinition target)
        {
            if (_testing || _testConnection is null || _selectedIndex < 0)
                return null;

            if (string.IsNullOrWhiteSpace(_nameBox.Text) || string.IsNullOrWhiteSpace(_addressBox.Text))
            {
                _computers.ShowFeedback("Complete the network name and server address in Network settings first.");
                return null;
            }

            ServerProfile profile = Clone(_profiles[_selectedIndex]);
            profile.Name = _nameBox.Text.Trim();
            profile.ServerAddress = _addressBox.Text.Trim();
            profile.PublicKey = _keyBox.Text.Trim();
            profile.RequiresPrivateNetwork = _privateNetworkBox.Checked;
            profile.ProbeHost = _probeHostBox.Text.Trim();
            profile.ProbePort = (int)_probePortBox.Value;
            _testing = true;
            WindowContent.Enabled = false;
            try
            {
                return await _testConnection(this, target, profile);
            }
            finally
            {
                WindowContent.Enabled = true;
                _testing = false;
            }
        }

        private void DeleteSelected()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _profiles.Count)
                return;

            if (_profiles.Count <= 1)
            {
                MessageBox.Show(this,
                                "At least one network profile must remain.",
                                "Cannot delete",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information
                               );
                return;
            }

            if (Targets.Any(t => t.ProfileId == _profiles[_selectedIndex].Id))
            {
                MessageBox.Show(this,
                                "Remove this network's computers and Save network before deleting it.",
                                "Network still has computers",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information
                               );
                return;
            }
            if (MessageBox.Show(this,
                                $"Delete “{_profiles[_selectedIndex].Name}”?",
                                "Delete profile",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question
                               )
                != DialogResult.Yes)
                return;

            _profiles.RemoveAt(_selectedIndex);
            RebindProfiles(_profiles.Count > 0 ? Math.Min(_selectedIndex, _profiles.Count - 1) : -1);
        }

        private void RebindProfiles(int index)
        {
            _networkListTransition.Begin();
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
            }
            _networkListTransition.End(true);
        }

        private static ServerProfile Clone(ServerProfile p) => new ServerProfile()
        {
            Id = p.Id,
            Name = p.Name,
            ServerAddress = p.ServerAddress,
            PublicKey = p.PublicKey,
            RequiresPrivateNetwork = p.RequiresPrivateNetwork,
            ProbeHost = p.ProbeHost,
            ProbePort = p.ProbePort,
        };
        #endregion
    }
}