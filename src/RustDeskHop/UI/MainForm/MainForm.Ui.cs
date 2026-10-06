using RustDeskHop.UI.Controls;
using RustDeskHop.UI.Theme;

namespace RustDeskHop.UI
{
    internal sealed partial class MainForm
    {
        #region Constants and Fields
        private readonly ComputerGrid _targetsGrid = new ComputerGrid
        {
            Name = "Computers",
            AccessibleName = "Saved computers",
            AccessibleDescription = "Choose Connect on a computer, or select its row and press Enter.",
            TabIndex = 0
        };
        private readonly ModernButton _profilesButton = new ModernButton
        {
            Name = "ManageNetworks",
            Text = "Manage computers && networks",
            AccessibleName = "Manage computers and networks",
            ForeColor = AppTheme.blue,
            Glyph = UiGlyph.SETTINGS,
            Accent = true,
            TabIndex = 1
        };
        private readonly Label _emptyState =
            AppTheme.Label("No computers yet\nAdd one in Manage computers & networks.", color: AppTheme.muted);
        #endregion

        #region Methods
        private void BuildUi()
        {
            WindowContent.BackColor = Color.White;
            SurfacePanel card = new SurfacePanel { Name = "ComputersCard", TabIndex = 0, Outlined = false };
            _emptyState.Name = "NoComputers";
            _emptyState.AutoSize = false;
            _emptyState.UseMnemonic = false;
            _emptyState.TextAlign = ContentAlignment.MiddleCenter;
            _emptyState.Visible = false;
            card.Controls.AddRange([_targetsGrid, _emptyState]);
            WindowContent.Controls.AddRange([card, _profilesButton]);
            bool layingOut = false;
            WindowContent.Layout += (_, _) =>
            {
                if (layingOut)
                    return;

                layingOut = true;
                try
                {
                    int Px(int value) => (int)Math.Round(value * UiScale.Factor(this));
                    int margin = Px(UiMetrics.PAGE_INSET);
                    int width = Math.Max(1,
                                         Math.Min(Px(UiMetrics.CONTENT_WIDTH),
                                                  WindowContent.ClientSize.Width - 2 * margin
                                                 )
                                        );
                    int left = (WindowContent.ClientSize.Width - width) / 2;
                    Size manageSize = _profilesButton.GetPreferredSize(Size.Empty);
                    int footerTop = WindowContent.ClientSize.Height - margin - manageSize.Height;
                    _profilesButton.SetBounds(left + width - manageSize.Width,
                                              footerTop,
                                              manageSize.Width,
                                              manageSize.Height
                                             );
                    int cardTop = margin;
                    int edge = Px(4);
                    int gridHeight = Math.Max(1, footerTop - Px(UiMetrics.FOOTER_GAP) - cardTop - 2 * edge);
                    card.SetBounds(left, cardTop, width, gridHeight + 2 * edge);
                    _targetsGrid.SetBounds(edge, edge, width - 2 * edge, gridHeight);
                    _emptyState.Bounds = _targetsGrid.Bounds;
                }
                finally
                {
                    layingOut = false;
                }
            };
            _targetsGrid.ContentHeightChanged += (_, _) => WindowContent.PerformLayout();
            _targetsGrid.ConnectRequested += async (_, e) => await ConnectRowAsync(e.RowIndex);
            _profilesButton.Click += (_, _) => ManageProfiles();
        }
        #endregion
    }
}