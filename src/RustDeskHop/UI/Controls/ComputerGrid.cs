using System.Drawing.Drawing2D;
using RustDeskHop.UI.Theme;

namespace RustDeskHop.UI.Controls
{
    internal sealed record ComputerRow(string Name, string RustDeskId, string ProfileName)
    {
        public string AccessibleIdentity => $"{Name}, RustDesk ID {ComputerIdentityPainter.DisplayId(RustDeskId)}";
    }

    internal sealed class ComputerGrid : DataGridView, IUiScaleAware
    {
        #region Constants and Fields
        private bool _sizingRows;
        private bool _sizingScheduled;
        private int _lastContentHeight;
        private int? _openingRow;
        #endregion

        #region Constructors and Destructors
        public ComputerGrid()
        {
            DoubleBuffered = true;
            BorderStyle = BorderStyle.None;
            BackgroundColor = Color.White;
            GridColor = Color.White;
            CellBorderStyle = DataGridViewCellBorderStyle.None;
            AdvancedCellBorderStyle.All = DataGridViewAdvancedCellBorderStyle.None;
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            EnableHeadersVisualStyles = false;
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToResizeRows = false;
            AllowUserToResizeColumns = false;
            ReadOnly = true;
            MultiSelect = false;
            RowHeadersVisible = false;
            ColumnHeadersVisible = false;
            AutoGenerateColumns = false;
            SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            ColumnHeadersHeight = UiMetrics.TABLE_HEADER_HEIGHT;
            RowTemplate.MinimumHeight = UiMetrics.COMPUTER_ROW_HEIGHT;
            Font = AppTheme.body;
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = AppTheme.ink,
                SelectionBackColor = AppTheme.selection,
                SelectionForeColor = AppTheme.ink,
                WrapMode = DataGridViewTriState.True,
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(UiMetrics.CELL_INSET, 8, UiMetrics.CELL_INSET, 8),
            };
            Columns.Add(new DataGridViewTextBoxColumn
                        {
                            Name = "Computer", HeaderText = "Computer", DataPropertyName = "AccessibleIdentity",
                            FillWeight = 60,
                            SortMode = DataGridViewColumnSortMode.NotSortable,
                            DefaultCellStyle = new DataGridViewCellStyle
                            {
                                Padding = new Padding(UiMetrics.COMPUTER_TEXT_INSET, 8, UiMetrics.CELL_INSET, 8),
                                Font = AppTheme.strong
                            }
                        }
                       );
            Columns.Add(new DataGridViewTextBoxColumn
                        {
                            Name = "RustDeskId", HeaderText = "RustDesk ID", DataPropertyName = "RustDeskId",
                            Visible = false,
                            FillWeight = 24,
                            SortMode = DataGridViewColumnSortMode.NotSortable
                        }
                       );
            Columns.Add(new DataGridViewTextBoxColumn
                        {
                            Name = "Network", HeaderText = "Network", DataPropertyName = "ProfileName", FillWeight = 40,
                            SortMode = DataGridViewColumnSortMode.NotSortable
                        }
                       );
            Columns.Add(new DataGridViewButtonColumn
                        {
                            Name = "Connect",
                            HeaderText = "Connect",
                            Text = "Connect",
                            UseColumnTextForButtonValue = true,
                            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                            Width = 116,
                            SortMode = DataGridViewColumnSortMode.NotSortable,
                            DefaultCellStyle = new DataGridViewCellStyle
                            {
                                Padding = new Padding(8, 6, 8, 6),
                                Alignment = DataGridViewContentAlignment.MiddleCenter
                            }
                        }
                       );
            DataBindingComplete += (_, _) =>
            {
                // Rebinding briefly clears the rows and can lay out an empty grid.
                // Notify the parent after measuring even if the final height is unchanged.
                _lastContentHeight = -1;
                foreach (DataGridViewRow row in Rows)
                {
                    if (row.DataBoundItem is ComputerRow computer)
                        row.Cells["Connect"].ToolTipText = $"Connect to {computer.Name} via {computer.ProfileName}";
                }
                ScheduleRowSizing();
            };
            ColumnWidthChanged += (_, _) => ScheduleRowSizing();
            SizeChanged += (_, _) => ScheduleRowSizing();
        }
        #endregion

        #region Delegates and Events
        internal event EventHandler? ContentHeightChanged;
        internal event DataGridViewCellEventHandler? ConnectRequested;
        #endregion

        #region Properties and Indexers
        internal int ContentHeight => Rows.Cast<DataGridViewRow>().Sum(row => row.Height);
        #endregion

        #region Methods
        internal void SetOpeningRow(int? index)
        {
            _openingRow = index;
            Enabled = index is null;
            foreach (DataGridViewRow row in Rows)
            {
                DataGridViewButtonCell button = (DataGridViewButtonCell)row.Cells["Connect"];
                button.UseColumnTextForButtonValue = false;
                button.Value = row.Index == index ? "Opening…" : "Connect";
            }
            Invalidate();
        }

        protected override void OnCellContentClick(DataGridViewCellEventArgs e)
        {
            base.OnCellContentClick(e);
            if (Enabled && e.RowIndex >= 0 && e.ColumnIndex == Columns["Connect"]!.Index)
                ConnectRequested?.Invoke(this, e);
        }

        protected override void OnCellDoubleClick(DataGridViewCellEventArgs e)
        {
            base.OnCellDoubleClick(e);
            if (Enabled && e.RowIndex >= 0 && e.ColumnIndex != Columns["Connect"]!.Index)
                ConnectRequested?.Invoke(this, e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (Enabled && e.KeyCode == Keys.Enter && CurrentCell is not null)
            {
                e.SuppressKeyPress = true;
                ConnectRequested?.Invoke(this,
                                         new DataGridViewCellEventArgs(CurrentCell.ColumnIndex, CurrentCell.RowIndex)
                                        );
                return;
            }
            base.OnKeyDown(e);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ScheduleRowSizing();
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            ApplyUiScale();
        }

        public void ApplyUiScale()
        {
            ColumnHeadersHeight = UiScale.Pixels(this, UiMetrics.TABLE_HEADER_HEIGHT);
            RowTemplate.MinimumHeight = UiScale.Pixels(this, UiMetrics.COMPUTER_ROW_HEIGHT);
            Columns["Connect"]!.Width = UiScale.Pixels(this, 116);
            Columns[0].DefaultCellStyle.Font = UiScale.FontFor(this, AppTheme.strong);
            foreach (DataGridViewRow row in Rows)
            {
                row.MinimumHeight = RowTemplate.MinimumHeight;
            }

            ScheduleRowSizing();
        }

        protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
        {
            base.OnCellPainting(e);
            if (e.ColumnIndex < 0)
                return;

            if (e.RowIndex < 0)
                PaintCell(e.Graphics!, e.CellBounds, e.RowIndex, e.ColumnIndex, e.FormattedValue?.ToString());
            e.Handled = true;
        }

        protected override void OnRowPrePaint(DataGridViewRowPrePaintEventArgs e)
        {
            base.OnRowPrePaint(e);
            e.PaintParts = DataGridViewPaintParts.None;
        }

        protected override void OnRowPostPaint(DataGridViewRowPostPaintEventArgs e)
        {
            base.OnRowPostPaint(e);
            GraphicsState graphicsState = e.Graphics.Save();
            e.Graphics.SetClip(new Rectangle(0,
                                             0,
                                             ClientSize.Width,
                                             ClientSize.Height
                                            ),
                               CombineMode.Intersect
                              );
            using (SolidBrush background = new SolidBrush(Rows[e.RowIndex].Selected ? AppTheme.selection : Color.White))
                e.Graphics.FillRectangle(background, e.RowBounds);
            for (int column = 0; column < Columns.Count; column++)
            {
                if (!Columns[column].Visible)
                    continue;

                Rectangle cellBounds = GetCellDisplayRectangle(column, e.RowIndex, false);
                PaintCell(e.Graphics,
                          cellBounds,
                          e.RowIndex,
                          column,
                          Rows[e.RowIndex].Cells[column].FormattedValue?.ToString()
                         );
            }

            RectangleF bounds = new RectangleF(1,
                                               e.RowBounds.Top,
                                               ClientSize.Width
                                               - (Controls.OfType<VScrollBar>().Any(s => s.Visible)
                                                   ? SystemInformation.VerticalScrollBarWidth
                                                   : 0)
                                               - 3,
                                               e.RowBounds.Height - 1
                                              );
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (Rows[e.RowIndex].Selected)
            {
                if (Focused && ShowFocusCues)
                {
                    ControlPaint.DrawFocusRectangle(e.Graphics,
                                                    Rectangle.Round(RectangleF.Inflate(bounds, -4, -4)),
                                                    AppTheme.muted,
                                                    AppTheme.selection
                                                   );
                }
            }

            using Pen separator = new Pen(AppTheme.line);
            e.Graphics.DrawLine(separator, bounds.Left, bounds.Bottom, bounds.Right, bounds.Bottom);

            e.Graphics.Restore(graphicsState);
        }

        private void ScheduleRowSizing()
        {
            if (!IsHandleCreated || IsDisposed || _sizingRows || _sizingScheduled)
                return;

            _sizingScheduled = true;

            // Fill-mode columns cannot change row heights from inside their resize event.
            // Coalesce requests and measure only after the grid's layout has completed.
            BeginInvoke((Action)(() =>
                        {
                            _sizingScheduled = false;
                            if (!IsDisposed)
                                SizeRowsToContent();
                        })
                       );
        }

        private void SizeRowsToContent()
        {
            if (_sizingRows || Columns.Count != 4)
                return;

            int previousWidth = Columns.Cast<DataGridViewColumn>().Sum(c => c.Width);
            _sizingRows = true;
            try
            {
                float scale = UiScale.Factor(this);
                int Px(float value) => (int)Math.Round(value * scale);
                int baseHeight = Px(UiMetrics.COMPUTER_ROW_HEIGHT);
                foreach (DataGridViewRow gridRow in Rows)
                {
                    if (gridRow.DataBoundItem is not ComputerRow row)
                        continue;

                    int Measure(string text, Font font, int width) => TextRenderer
                        .MeasureText(text,
                                     font,
                                     new Size(Math.Max(Px(40), width), int.MaxValue),
                                     TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix
                                    )
                        .Height;

                    int textHeight =
                        Math.Max(Measure(row.Name,
                                         UiScale.FontFor(this, AppTheme.strong),
                                         Columns[0].Width - Px(UiMetrics.IDENTITY_INSET + UiMetrics.CELL_INSET)
                                        ) + UiScale.FontFor(this, AppTheme.small).Height + Px(3),
                                 Measure(row.ProfileName, Font, Columns[2].Width - Px(2 * UiMetrics.CELL_INSET))
                                );
                    gridRow.Height = Math.Max(baseHeight, textHeight + Px(24));
                }
            }
            finally
            {
                _sizingRows = false;
            }

            if (previousWidth != Columns.Cast<DataGridViewColumn>().Sum(c => c.Width))
                ScheduleRowSizing();
            if (_lastContentHeight != ContentHeight)
            {
                _lastContentHeight = ContentHeight;
                ContentHeightChanged?.Invoke(this, EventArgs.Empty);
            }

            Invalidate();
        }

        private void PaintCell(Graphics graphics, Rectangle bounds, int rowIndex, int columnIndex, string? text)
        {
            float scale = UiScale.Factor(this);
            int Px(float value) => (int)Math.Round(value * scale);
            bool selected = rowIndex >= 0 && Rows[rowIndex].Selected;
            using SolidBrush background = new SolidBrush(selected ? AppTheme.selection : Color.White);
            graphics.FillRectangle(background, bounds);
            if (rowIndex < 0)
            {
                // Keep the accessible column name, without repeating each button's visible label.
                if (columnIndex == Columns["Connect"]!.Index)
                    return;

                Rectangle headerBounds = Rectangle.Inflate(bounds, -Px(UiMetrics.CELL_INSET), 0);
                if (columnIndex == 0)
                {
                    headerBounds.X = bounds.Left + Px(UiMetrics.COMPUTER_TEXT_INSET);
                    headerBounds.Width = bounds.Right - headerBounds.X;
                }

                TextRenderer.DrawText(graphics,
                                      text,
                                      AppTheme.small,
                                      headerBounds,
                                      AppTheme.muted,
                                      TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix
                                     );
            }
            else
            {
                Rectangle textBounds = Rectangle.Inflate(bounds, -Px(UiMetrics.CELL_INSET), -Px(8));
                if (columnIndex == 0)
                {
                    ComputerIdentityPainter.Draw(graphics,
                                                 this,
                                                 bounds,
                                                 ((ComputerRow)Rows[rowIndex].DataBoundItem!).Name,
                                                 Rows[rowIndex].Cells["RustDeskId"].Value?.ToString() ?? "",
                                                 scale
                                                );
                    return;
                }

                if (columnIndex == Columns["Connect"]!.Index)
                {
                    Rectangle button = new Rectangle(bounds.Left + Px(8),
                                                     bounds.Top + (bounds.Height - Px(UiMetrics.BUTTON_HEIGHT)) / 2,
                                                     bounds.Width - Px(16),
                                                     Px(UiMetrics.BUTTON_HEIGHT)
                                                    );
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using GraphicsPath shape = AppTheme.Round(button, Px(7));
                    bool opening = _openingRow == rowIndex;
                    using SolidBrush brush = new SolidBrush(Enabled ? AppTheme.blue : AppTheme.badge);
                    using Pen outline = new Pen(Enabled ? AppTheme.blue : AppTheme.line);
                    graphics.FillPath(brush, shape);
                    graphics.DrawPath(outline, shape);
                    TextRenderer.DrawText(graphics,
                                          opening ? "Opening…" : "Connect",
                                          Font,
                                          button,
                                          Enabled ? Color.White : AppTheme.muted,
                                          TextFormatFlags.HorizontalCenter
                                          | TextFormatFlags.VerticalCenter
                                          | TextFormatFlags.NoPadding
                                         );
                    if (Focused
                        && ShowFocusCues
                        && CurrentCell?.RowIndex == rowIndex
                        && CurrentCell.ColumnIndex == columnIndex)
                        ControlPaint.DrawFocusRectangle(graphics,
                                                        Rectangle.Inflate(button, -Px(4), -Px(4)),
                                                        Color.White,
                                                        AppTheme.blue
                                                       );
                    return;
                }

                TextRenderer.DrawText(graphics,
                                      text,
                                      columnIndex == 0 ? AppTheme.strong : Font,
                                      textBounds,
                                      columnIndex == 1 ? AppTheme.muted : AppTheme.ink,
                                      TextFormatFlags.Left
                                      | TextFormatFlags.VerticalCenter
                                      | TextFormatFlags.WordBreak
                                      | TextFormatFlags.NoPrefix
                                     );
            }
        }
        #endregion
    }
}