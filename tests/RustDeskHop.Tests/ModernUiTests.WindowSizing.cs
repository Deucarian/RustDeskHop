using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Xunit;

namespace RustDeskHop.Tests
{
    public sealed partial class ModernUiTests
    {
        #region Test Methods
        [Fact]
        public void DashboardResizesAndFullscreenRestoresItsPreviousBounds()
        {
            OnSta(() =>
            {
                using MainForm form = TestApplication.CreateMainForm(Settings(30));
                form.Show();
                Application.DoEvents();
                ComputerGrid grid = Find<ComputerGrid>(form, "Computers");
                Size smallGrid = grid.Size;
                form.ClientSize = new Size(1100, 700);
                Application.DoEvents();
                Assert.True(grid.Width > smallGrid.Width);
                Assert.True(grid.Height > smallGrid.Height);
                Assert.True(grid.DisplayedRowCount(false) > 3);
                Rectangle bounds = form.Bounds;
                PressWindowKey(form, Keys.F11);
                Application.DoEvents();
                Assert.Equal(FormBorderStyle.None, form.FormBorderStyle);
                Assert.Equal(Screen.FromControl(form).Bounds, form.Bounds);
                Message message = new Message();
                typeof(MainForm).GetMethod("ProcessCmdKey", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(form, new object[] { message, Keys.Escape });
                Application.DoEvents();
                Assert.Equal(FormBorderStyle.Sizable, form.FormBorderStyle);
                Assert.Equal(bounds, form.Bounds);
                Assert.True(form.MaximizeBox);
            });
        }

        [Fact]
        public void FullscreenReturnsToMaximizedWindow()
        {
            OnSta(() =>
            {
                using MainForm form = TestApplication.CreateMainForm(Settings(3));
                form.Show();
                form.WindowState = FormWindowState.Maximized;
                Application.DoEvents();
                PressWindowKey(form, Keys.F11);
                Application.DoEvents();
                Assert.Equal(Screen.FromControl(form).Bounds, form.Bounds);
                PressWindowKey(form, Keys.F11);
                Application.DoEvents();
                Assert.Equal(FormWindowState.Maximized, form.WindowState);
            });
        }

        [Fact]
        public void ClickingAnUnfocusedNetworkListPreservesItsViewport()
        {
            OnSta(() =>
            {
                using Form form = new Form();
                using HoverListBox list = new HoverListBox { Dock = DockStyle.Top, Height = 120 };
                using Button other = new Button { Dock = DockStyle.Bottom };
                for (int index = 0; index < 30; index++)
                    list.Items.Add("Network " + index);
                form.Controls.AddRange([list, other]);
                form.Show();
                list.SelectedIndex = 0;
                other.Focus();
                list.TopIndex = 15;
                int top = list.TopIndex;
                Rectangle row = list.GetItemRectangle(top + 1);
                int y = row.Top + row.Height / 2;
                Message message = Message.Create(list.Handle, 0x0201, new IntPtr(1), new IntPtr((y << 16) | 10));
                typeof(HoverListBox).GetMethod("WndProc", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(list, new object[] { message });
                message = Message.Create(list.Handle, 0x0202, IntPtr.Zero, message.LParam);
                typeof(HoverListBox).GetMethod("WndProc", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(list, new object[] { message });
                Application.DoEvents();
                Assert.Equal(top, list.TopIndex);
                Assert.Equal(top + 1, list.SelectedIndex);
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ClickingAnUnfocusedScrolledGridKeepsTheClickedRow(bool dashboard)
        {
            OnSta(() =>
            {
                using Form form = dashboard ? TestApplication.CreateMainForm(Settings(30)) : new Form();
                DataGridView grid;
                if (dashboard)
                    grid = Find<ComputerGrid>(form, "Computers");
                else
                {
                    grid = new RowActionGrid { Dock = DockStyle.Top, Height = 160, AllowUserToAddRows = false };
                    grid.Columns.Add("Name", "Name");
                    for (int index = 0; index < 30; index++)
                        grid.Rows.Add("Computer " + index);
                    form.Controls.Add(grid);
                }
                Button other = new Button { Dock = DockStyle.Bottom, Text = "Other" };
                form.Controls.Add(other);
                form.Show();
                Application.DoEvents();
                grid.CurrentCell = grid.Rows[0].Cells[0];
                other.Focus();
                grid.FirstDisplayedScrollingRowIndex = 15;
                int first = grid.FirstDisplayedScrollingRowIndex;
                Rectangle cell = grid.GetCellDisplayRectangle(0, first + 1, false);
                int x = cell.Left + 10;
                int y = cell.Top + cell.Height / 2;
                Message message = Message.Create(grid.Handle, 0x0201, new IntPtr(1), new IntPtr((y << 16) | x));
                typeof(PointerFocusGrid).GetMethod("WndProc", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(grid, new object[] { message });
                message = Message.Create(grid.Handle, 0x0202, IntPtr.Zero, message.LParam);
                typeof(PointerFocusGrid).GetMethod("WndProc", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(grid, new object[] { message });
                Application.DoEvents();
                Assert.Equal(first, grid.FirstDisplayedScrollingRowIndex);
                Assert.Equal(first + 1, grid.CurrentCell!.RowIndex);
            });
        }
        #endregion

        #region Methods
        private static void PressWindowKey(MainForm form, Keys key)
        {
            Message message = new Message();
            typeof(MainForm).GetMethod("ProcessCmdKey", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(form, new object[] { message, key });
        }
        #endregion
    }
}
