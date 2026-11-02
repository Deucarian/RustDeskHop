using RustDeskHop.UI.Controls;
using RustDeskHop.UI.Theme;

namespace RustDeskHop.UI
{
    internal sealed partial class NetworkComputersEditor
    {
        #region Methods
        private void BuildUi()
        {
            Name = "NetworkComputersEditor";
            AccessibleName = "Computers in the selected network";
            BackColor = Color.White;
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Panel content = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty
            };
            _grid.Name = "NetworkComputers";
            _grid.AccessibleName = "Computers in this network";
            _grid.AccessibleDescription =
                "Edit names directly. Each row has Test and Remove actions. The final row adds a computer.";
            _grid.Dock = DockStyle.Fill;
            _grid.BackgroundColor = Color.White;
            _grid.BorderStyle = BorderStyle.None;
            _grid.GridColor = AppTheme.line;
            _grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            _grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            _grid.EnableHeadersVisualStyles = false;
            _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = AppTheme.canvas,
                ForeColor = AppTheme.muted,
                Font = AppTheme.small,
            };
            _grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = AppTheme.body,
                ForeColor = AppTheme.ink,
                BackColor = Color.White,
                SelectionBackColor = Color.White,
                SelectionForeColor = AppTheme.ink,
                Padding = new Padding(UiMetrics.GAP),
                WrapMode = DataGridViewTriState.True,
                Alignment = DataGridViewContentAlignment.MiddleLeft,
            };
            _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            _grid.RowTemplate.MinimumHeight = UiMetrics.COMPUTER_ROW_HEIGHT;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.AllowUserToResizeColumns = false;
            _grid.MultiSelect = false;
            _grid.RowHeadersVisible = false;
            _grid.ColumnHeadersVisible = false;
            _grid.AutoGenerateColumns = false;
            _grid.EditMode = DataGridViewEditMode.EditOnEnter;
            _grid.Columns.Add(new DataGridViewTextBoxColumn
                              {
                                  Name = "ComputerName", HeaderText = "Computer name",
                                  CellTemplate = new ComputerEditorCell(),
                                  FillWeight = 60, SortMode = DataGridViewColumnSortMode.NotSortable,
                              }
                             );
            _grid.Columns.Add(new DataGridViewTextBoxColumn
                              {
                                  Name = "RustDeskId", HeaderText = "RustDesk ID",
                                  CellTemplate = new ComputerEditorCell(),
                                  FillWeight = 40, SortMode = DataGridViewColumnSortMode.NotSortable,
                              }
                             );
            _grid.Columns.Add(new DataGridViewButtonColumn
                              {
                                  CellTemplate = new RowActionCell(),
                                  Name = "TestComputer", HeaderText = "Test", Width = 92, ReadOnly = true,
                                  AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                                  SortMode = DataGridViewColumnSortMode.NotSortable
                              }
                             );
            _grid.Columns.Add(new DataGridViewButtonColumn
                              {
                                  CellTemplate = new RowActionCell { Quiet = true },
                                  Name = "RemoveComputer", HeaderText = "Remove", Width = 76, ReadOnly = true,
                                  AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                                  SortMode = DataGridViewColumnSortMode.NotSortable
                              }
                             );
            content.Controls.Add(_grid);
            layout.Controls.Add(content, 0, 0);
            _feedback.Name = "ComputerFeedback";
            _feedback.AccessibleName = "Computer validation and test result";
            _feedback.Dock = DockStyle.Top;
            _feedback.Margin = new Padding(0, UiMetrics.GAP, 0, 0);
            _feedback.Visible = false;
            layout.SizeChanged += (_, _) => _feedback.MaximumSize = new Size(Math.Max(1, layout.ClientSize.Width), 0);
            layout.Controls.Add(_feedback, 0, 1);
            _grid.CellContentClick += async (_, e) => await ActivateCellAsync(e.RowIndex, e.ColumnIndex);
            _grid.CellEndEdit += (_, e) => CommitCell(e.RowIndex);
            Controls.Add(layout);
            UpdateActions();
        }
        #endregion
    }
}