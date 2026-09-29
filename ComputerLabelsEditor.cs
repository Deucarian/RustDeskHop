using System.ComponentModel;

namespace Simultria.RustDeskCompanion;

// Draft labels are isolated from settings until the existing Save network action.
internal sealed class ComputerLabelsEditor : UserControl
{
    private readonly DataGridView grid = new();
    private readonly Label empty = AppTheme.Label("No saved computers use this network yet.", AppTheme.Body, AppTheme.Muted);
    private BindingList<LabelDraft> drafts = [];

    internal ComputerLabelsEditor()
    {
        Name = "ComputerLabelsEditor";
        BackColor = Color.White;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var hint = AppTheme.Label("Edit a name, then choose Save network. Only the label in RustDeskHop changes.", AppTheme.Small, AppTheme.Muted);
        hint.Dock = DockStyle.Top;
        hint.Margin = new Padding(0, 0, 0, UiMetrics.Inset);
        layout.SizeChanged += (_, _) => hint.MaximumSize = new Size(Math.Max(1, layout.ClientSize.Width), 0);
        layout.Controls.Add(hint, 0, 0);
        var content = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        grid.Name = "ComputerLabels";
        grid.AccessibleName = "Computer names for this network";
        grid.AccessibleDescription = "Edit the Computer name column. RustDesk IDs cannot be changed here. Save using Save network.";
        grid.Dock = DockStyle.Fill;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.GridColor = AppTheme.Line;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = AppTheme.Canvas, ForeColor = AppTheme.Muted, Font = AppTheme.Small,
        };
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            Font = AppTheme.Body, ForeColor = AppTheme.Ink, BackColor = Color.White,
            SelectionBackColor = AppTheme.Selection, SelectionForeColor = AppTheme.Ink,
            Padding = new Padding(UiMetrics.Gap), WrapMode = DataGridViewTriState.True,
        };
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        grid.RowTemplate.MinimumHeight = UiMetrics.RowHeight;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.AllowUserToResizeColumns = false;
        grid.MultiSelect = false;
        grid.RowHeadersVisible = false;
        grid.AutoGenerateColumns = false;
        grid.EditMode = DataGridViewEditMode.EditOnEnter;
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ComputerName", HeaderText = "Computer name", DataPropertyName = nameof(LabelDraft.Name),
            FillWeight = 65, SortMode = DataGridViewColumnSortMode.NotSortable,
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "RustDeskId", HeaderText = "RustDesk ID", DataPropertyName = nameof(LabelDraft.RustDeskId),
            ReadOnly = true, FillWeight = 35, SortMode = DataGridViewColumnSortMode.NotSortable,
        });
        empty.Name = "NoNetworkComputers";
        empty.Dock = DockStyle.Fill;
        empty.AutoSize = false;
        content.Controls.Add(grid);
        content.Controls.Add(empty);
        layout.Controls.Add(content, 0, 1);
        Controls.Add(layout);
        LoadTargets([]);
    }

    internal void LoadTargets(IEnumerable<TargetDefinition> targets)
    {
        // Switching networks discards unsaved edits, just like the network fields.
        grid.CancelEdit();
        drafts = new BindingList<LabelDraft>(targets.Select(t => new LabelDraft(t)).ToList());
        grid.DataSource = drafts;
        grid.Visible = drafts.Count > 0;
        empty.Visible = drafts.Count == 0;
    }

    internal bool TrySave(out string? error)
    {
        if (!grid.EndEdit())
        {
            error = "Finish editing the computer name before saving.";
            return false;
        }
        if (grid.BindingContext?[drafts] is CurrencyManager manager) manager.EndCurrentEdit();
        if (drafts.Any(d => string.IsNullOrWhiteSpace(d.Name)))
        {
            error = "Every computer needs a name. No changes were saved.";
            return false;
        }
        foreach (var draft in drafts) draft.Target.Name = draft.Name.Trim();
        error = null;
        return true;
    }

    private sealed class LabelDraft(TargetDefinition target)
    {
        internal TargetDefinition Target { get; } = target;
        public string Name { get; set; } = target.Name;
        public string RustDeskId => Target.RustDeskId;
    }
}
