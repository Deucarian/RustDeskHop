using System.ComponentModel;
using RustDeskHop.UI.Theme;

namespace RustDeskHop.UI.Controls
{
    internal sealed class SectionTabs : UserControl, IUiScaleAware
    {
        #region Constants and Fields
        private readonly List<Control> _pages = [];
        private readonly List<ModernButton> _buttons = [];
        private readonly SurfacePanel _track = new SurfacePanel { BackColor = Color.White, Outlined = false };
        private readonly Panel _pageHost = new Panel { BackColor = Color.White };
        private int _selectedIndex;
        private readonly ContentTransition _transition;
        #endregion

        #region Constructors and Destructors
        internal SectionTabs()
        {
            _transition = new ContentTransition(_pageHost);
            BackColor = Color.White;
            AutoScaleMode = AutoScaleMode.None;
            Controls.Add(_track);
            Controls.Add(_pageHost);
            AccessibleRole = AccessibleRole.PageTabList;
            TabStop = false;
        }
        #endregion

        #region Delegates and Events
        internal event EventHandler? SelectedIndexChanged;
        #endregion

        #region Properties and Indexers
        internal int TabCount => _pages.Count;
        internal ContentTransition Transition => _transition;
        internal Control? SelectedTab => _pages.ElementAtOrDefault(_selectedIndex);
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (value < 0 || value >= _pages.Count || value == _selectedIndex)
                    return;

                _transition.Begin();
                _selectedIndex = value;
                UpdateSelection();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                _transition.End();
            }
        }
        #endregion

        #region Methods
        internal void AddPage(Control page)
        {
            int index = _pages.Count;
            _pages.Add(page);
            _pageHost.Controls.Add(page);
            ModernButton button = new ModernButton
            {
                Text = page.Text,
                AccessibleName = page.Text,
                AccessibleRole = AccessibleRole.PageTab,
                TabSegment = true,
                Accent = true,
                Margin = Padding.Empty,
                TabIndex = 1 - index
            };
            button.Click += (_, _) =>
            {
                SelectedIndex = index;
                button.Focus();
            };
            button.KeyDown += (_, e) =>
            {
                if (e.KeyCode is Keys.Left or Keys.Right)
                {
                    e.SuppressKeyPress = true;
                    SelectedIndex = e.KeyCode == Keys.Left ? 1 : 0;
                    _buttons[SelectedIndex].Focus();
                }
            };
            _buttons.Add(button);
            _track.Controls.Add(button);
            UpdateSelection();
            PerformLayout();
        }

        protected override bool ProcessCmdKey(ref Message message, Keys keyData)
        {
            if (keyData is (Keys.Control | Keys.Tab) or (Keys.Control | Keys.Shift | Keys.Tab))
            {
                SelectedIndex = 1 - SelectedIndex;
                _buttons[SelectedIndex].Focus();
                return true;
            }
            return base.ProcessCmdKey(ref message, keyData);
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (_track is null)
                return;

            int Px(int value) => UiScale.Pixels(this, value);
            int labelWidth = _buttons.Count == 0 ? Px(136)
                : _buttons.Max(button => TextRenderer.MeasureText(button.Text, button.Font).Width + Px(28));
            int trackWidth = Math.Min(ClientSize.Width, Math.Max(Px(280), labelWidth * 2 + Px(8)));
            _track.SetBounds(0, 0, trackWidth, Px(36));
            _pageHost.SetBounds(0, Px(48), ClientSize.Width, Math.Max(1, ClientSize.Height - Px(48)));
            for (int index = 0; index < _buttons.Count; index++)
            {
                int width = (_track.Width - Px(6)) / 2;
                _buttons[index].SetBounds(Px(3) + (1 - index) * width, Px(3), width - Px(4), Px(30));
                _pages[index].Bounds = _pageHost.ClientRectangle;
            }
        }

        private void UpdateSelection()
        {
            for (int index = 0; index < _pages.Count; index++)
            {
                bool selected = index == _selectedIndex;
                _pages[index].Visible = selected;
                _buttons[index].Quiet = false;
                _buttons[index].SelectedTab = selected;
                _buttons[index].TabStop = selected;
                _buttons[index].Font = UiScale.FontFor(this, AppTheme.body);
                _buttons[index].AccessibleDescription = selected ? "Selected" : "Not selected";
                _buttons[index].Invalidate();
            }
        }

        public void ApplyUiScale()
        {
            UpdateSelection();
            PerformLayout();
        }
        #endregion
    }
}