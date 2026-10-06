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
        public void DashboardHasNoPersistentSelectionOrDoubleClickAction()
        {
            OnSta(() =>
                  {
                      using MainForm form = TestApplication.CreateMainForm(Settings(3));
                      form.Show();
                      Application.DoEvents();
                      ComputerGrid grid = Find<ComputerGrid>(form, "Computers");
                      int requests = 0;
                      grid.ConnectRequested += (_, _) => requests++;
                      grid.CurrentCell = grid.Rows[1].Cells[0];
                      grid.Rows[1].Selected = true;
                      grid.Rows[1].Cells[0].Selected = true;
                      Assert.Empty(grid.SelectedRows);
                      Assert.Empty(grid.SelectedCells);
                      typeof(DataGridView)
                          .GetMethod("OnCellDoubleClick", BindingFlags.Instance | BindingFlags.NonPublic)!
                          .Invoke(grid, [new DataGridViewCellEventArgs(0, 1)]);
                      Assert.Equal(0, requests);
                  }
                 );
        }

        [Fact]
        public void TabArrowsReachTheSelectionHandlerInsteadOfOnlyMovingDialogFocus()
        {
            OnSta(() =>
                  {
                      using ModernButton tab = new ModernButton { TabSegment = true };
                      MethodInfo isInputKey = typeof(ModernButton)
                          .GetMethod("IsInputKey", BindingFlags.Instance | BindingFlags.NonPublic)!;
                      Assert.True((bool)isInputKey.Invoke(tab, [Keys.Left])!);
                      Assert.True((bool)isInputKey.Invoke(tab, [Keys.Right])!);
                  }
                 );
        }

        [Theory]
        [InlineData("ProcessDialogKey")]
        [InlineData("ProcessDataGridViewKey")]
        public void KeyboardTabsThroughConnectButtonsAndOutToManagement(string inputMethod)
        {
            OnSta(() =>
                  {
                      using MainForm form = TestApplication.CreateMainForm(Settings(3));
                      form.Show();
                      Application.DoEvents();
                      ComputerGrid grid = Find<ComputerGrid>(form, "Computers");
                      grid.Focus();
                      grid.CurrentCell = grid.Rows[0].Cells["Connect"];
                      PressConnectKey(grid, Keys.Tab, inputMethod);
                      Assert.Equal(grid.Rows[1].Cells["Connect"], grid.CurrentCell);
                      PressConnectKey(grid, Keys.Tab | Keys.Shift, inputMethod);
                      Assert.Equal(grid.Rows[0].Cells["Connect"], grid.CurrentCell);
                      grid.CurrentCell = grid.Rows[2].Cells["Connect"];
                      PressConnectKey(grid, Keys.Tab, inputMethod);
                      Assert.True(Find<ModernButton>(form, "ManageNetworks").Focused);
                      Assert.Empty(grid.SelectedCells);
                  }
                 );
        }

        [Theory]
        [InlineData(Keys.Enter)]
        [InlineData(Keys.Space)]
        public void KeyboardConnectInvokesTheFocusedComputerExactlyOnce(Keys key)
        {
            OnSta(() =>
                  {
                      using MainForm form = TestApplication.CreateMainForm(Settings(3));
                      form.Show();
                      Application.DoEvents();
                      ComputerGrid grid = Find<ComputerGrid>(form, "Computers");
                      List<int> requested = [];
                      grid.ConnectRequested += (_, e) => requested.Add(e.RowIndex);
                      grid.Focus();
                      grid.CurrentCell = grid.Rows[2].Cells["Connect"];
                      PressConnectKey(grid, key);
                      Assert.Equal([2], requested);
                      grid.Enabled = false;
                      PressConnectKey(grid, key);
                      Assert.Equal([2], requested);
                  }
                 );
        }

        [Fact]
        public void TabsShareOneContinuousFrameAndOnlyTheActivePageHasAnUnderline()
        {
            OnSta(() =>
                  {
                      using Bitmap computers = RenderTabStrip(true);
                      using Bitmap networks = RenderTabStrip(false);
                      Assert.Equal(AppTheme.blue.ToArgb(), computers.GetPixel(70, 34).ToArgb());
                      Assert.Equal(AppTheme.canvas.ToArgb(), computers.GetPixel(210, 32).ToArgb());
                      Assert.Equal(AppTheme.blue.ToArgb(), networks.GetPixel(210, 34).ToArgb());
                      Assert.Equal(AppTheme.canvas.ToArgb(), networks.GetPixel(70, 32).ToArgb());
                      Assert.Equal(Color.White.ToArgb(), computers.GetPixel(138, 4).ToArgb());
                      Assert.Equal(AppTheme.canvas.ToArgb(), computers.GetPixel(142, 4).ToArgb());
                      Assert.Equal(computers.GetPixel(70, 0), computers.GetPixel(139, 0));
                      Assert.Equal(computers.GetPixel(139, 0), computers.GetPixel(140, 0));
                      Assert.Equal(computers.GetPixel(140, 0), computers.GetPixel(210, 0));
                      Assert.Equal(Color.White.ToArgb(), computers.GetPixel(0, 0).ToArgb());
                  }
                 );
        }

        [Theory]
        [InlineData(50)]
        [InlineData(75)]
        [InlineData(90)]
        [InlineData(100)]
        [InlineData(125)]
        [InlineData(150)]
        public void TabHeadersTouchAndFillTheSharedStripAtEveryDensity(int percent)
        {
            OnSta(() =>
                  {
                      using UiScaleState scale = new UiScaleState(percent);
                      using ProfilesForm form = new ProfilesForm(Settings(1).Profiles);
                      form.UseScaleState(scale);
                      form.Show();
                      Application.DoEvents();
                      SectionTabs sections = Find<SectionTabs>(form, "NetworkSections");
                      ModernButton[] tabs = Descendants(sections).OfType<ModernButton>()
                          .OrderBy(button => button.Left).ToArray();
                      Assert.Equal(0, tabs[0].Left);
                      Assert.Equal(tabs[0].Right, tabs[1].Left);
                      Assert.Equal(tabs[0].Top, tabs[1].Top);
                      Assert.Equal(tabs[0].Height, tabs[1].Height);
                      Assert.Equal(sections.ClientSize.Width, tabs[1].Right);
                      Assert.All(tabs,
                                 tab => Assert.True(TextRenderer.MeasureText(tab.Text, tab.Font).Width < tab.Width)
                                );
                      Rectangle[] bounds = tabs.Select(tab => tab.Bounds).ToArray();
                      sections.SelectedIndex = 0;
                      Assert.Equal(bounds, tabs.Select(tab => tab.Bounds).ToArray());
                      Assert.Single(tabs, tab => tab.TabStop && tab.SelectedTab);
                  }
                 );
        }
        #endregion

        #region Methods
        private static Bitmap RenderTabStrip(bool computersSelected)
        {
            Bitmap bitmap = new Bitmap(280, 36);
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.White);
            for (int index = 0; index < 2; index++)
            {
                Rectangle header = new Rectangle(index * 140, 0, 140, 36);
                graphics.SetClip(header);
                PageTabPainter.Draw(graphics,
                                    header,
                                    new Rectangle(0, 0, 280, 36),
                                    1,
                                    (index == 0) == computersSelected,
                                    true,
                                    0
                                   );
            }
            return bitmap;
        }

        private static void PressConnectKey(ComputerGrid grid, Keys key, string inputMethod = "ProcessDialogKey")
        {
            object argument = inputMethod == "ProcessDialogKey" ? key : new KeyEventArgs(key);
            typeof(ComputerGrid).GetMethod(inputMethod, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(grid, [argument]);
            Application.DoEvents();
        }
        #endregion
    }
}