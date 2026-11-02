using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using RustDeskHop.UI.Controls;
using Xunit;

namespace RustDeskHop.Tests
{
    public sealed partial class ModernUiTests
    {
        #region Test Methods
        [Theory]
        [InlineData(DrawItemState.None)]
        [InlineData(DrawItemState.Selected | DrawItemState.Focus)]
        public void SidebarPublishesCompletedRowsWithoutExposingTheirBackgroundErase(DrawItemState state)
        {
            OnSta(() =>
                  {
                      using HoverListBox list = new HoverListBox();
                      using Bitmap target = new Bitmap(120, 100);
                      using Graphics graphics = Graphics.FromImage(target);
                      graphics.Clear(Color.Magenta);
                      Rectangle bounds = new Rectangle(0, 36, 120, 36);
                      list.DrawItem += (_, e) =>
                      {
                          Assert.NotSame(graphics, e.Graphics);
                          Assert.Equal(new Rectangle(Point.Empty, bounds.Size), e.Bounds);
                          Assert.Equal(state, e.State);
                          e.Graphics.FillRectangle(Brushes.White, e.Bounds);
                          Assert.Equal(Color.Magenta.ToArgb(), target.GetPixel(20, 50).ToArgb());
                          e.Graphics.FillRectangle(Brushes.Blue, e.Bounds);
                      };
                      using DrawItemEventArgs args = new DrawItemEventArgs(graphics, list.Font, bounds, 1, state);
                      InvokeSidebar(list, "OnDrawItem", args);
                      Assert.Equal(Color.Blue.ToArgb(), target.GetPixel(20, 50).ToArgb());
                      Assert.Equal(Color.Magenta.ToArgb(), target.GetPixel(20, 20).ToArgb());
                      Assert.Equal(Color.Magenta.ToArgb(), target.GetPixel(20, 90).ToArgb());
                  }
                 );
        }

        [Fact]
        public void SidebarRapidHoverRetargetsFromCurrentValuesWithoutChangingSelection()
        {
            OnSta(() =>
                  {
                      using HoverListBox list = SidebarList();
                      list.SelectedIndex = 2;
                      int selectionChanges = 0;
                      list.SelectedIndexChanged += (_, _) => selectionChanges++;
                      MotionTween motion = SidebarMotion(list);
                      HoverSidebar(list, 0);
                      motion.Advance(.25F);
                      float first = list.HoverAmount(0);
                      Assert.InRange(first, .01F, .99F);
                      HoverSidebar(list, 1);
                      Assert.Equal(first, list.HoverAmount(0));
                      Assert.Equal(0, list.HoverAmount(1));
                      motion.Advance(.25F);
                      float fading = list.HoverAmount(0);
                      float incoming = list.HoverAmount(1);
                      HoverSidebar(list, 2);
                      Assert.Equal(fading, list.HoverAmount(0));
                      Assert.Equal(incoming, list.HoverAmount(1));
                      Assert.Equal(0, list.HoverAmount(2));
                      motion.Advance(.25F);
                      float[] beforeLeave = Enumerable.Range(0, 3).Select(list.HoverAmount).ToArray();
                      InvokeSidebar(list, "OnMouseLeave", EventArgs.Empty);
                      Assert.Equal(beforeLeave, Enumerable.Range(0, 3).Select(list.HoverAmount));
                      motion.Finish();
                      Assert.All(Enumerable.Range(0, 3), index => Assert.Equal(0, list.HoverAmount(index)));
                      Assert.False(motion.IsRunning);
                      Assert.Equal(Cursors.Default, list.Cursor);
                      Assert.Equal(2, list.SelectedIndex);
                      Assert.Equal(0, selectionChanges);
                  }
                 );
        }

        [Fact]
        public void SidebarHoverInvalidatesOnlyChangingRowsWithoutNativeEraseOrIdleRedraws()
        {
            OnSta(() =>
                  {
                      using Form form = new Form();
                      using HoverListBox list = SidebarList();
                      form.Controls.Add(list);
                      list.DrawItem += (_, e) => e.DrawBackground();
                      form.Show();
                      Application.DoEvents();
                      MotionTween motion = SidebarMotion(list);
                      InvokeSidebar(list, "OnMouseLeave", EventArgs.Empty);
                      motion.Finish();
                      list.Update();
                      using SidebarPaintObserver observer = new SidebarPaintObserver(list);
                      List<Rectangle> invalidated = new List<Rectangle>();
                      list.Invalidated += (_, e) => invalidated.Add(e.InvalidRect);
                      HoverSidebar(list, 0);
                      motion.Advance(.5F);
                      list.Update();
                      HoverSidebar(list, 1);
                      motion.Advance(.5F);
                      list.Update();
                      InvokeSidebar(list, "OnMouseLeave", EventArgs.Empty);
                      motion.Finish();
                      list.Update();
                      Assert.NotEmpty(invalidated);
                      Assert.All(invalidated, bounds =>
                      {
                          Assert.True(bounds == list.GetItemRectangle(0) || bounds == list.GetItemRectangle(1));
                          Assert.NotEqual(list.ClientRectangle, bounds);
                      });
                      Assert.Equal(0, observer.BackgroundErases);
                      Assert.False(motion.IsRunning);
                      int count = invalidated.Count;
                      motion.Finish();
                      Assert.Equal(count, invalidated.Count);
                  }
                 );
        }

        [Fact]
        public void SidebarRebindingAndDisablingClearHoverAndStopAnimation()
        {
            OnSta(() =>
                  {
                      using HoverListBox list = SidebarList();
                      MotionTween motion = SidebarMotion(list);
                      HoverSidebar(list, 0);
                      motion.Finish();
                      list.Enabled = false;
                      Assert.Equal(0, list.HoverAmount(0));
                      Assert.False(motion.IsRunning);
                      HoverSidebar(list, 1);
                      Assert.Equal(Cursors.Default, list.Cursor);
                      Assert.False(motion.IsRunning);
                      list.Enabled = true;
                      HoverSidebar(list, 1);
                      motion.Advance(.5F);
                      list.DataSource = new[] { "Replacement network" };
                      Assert.All(Enumerable.Range(0, 3), index => Assert.Equal(0, list.HoverAmount(index)));
                      Assert.False(motion.IsRunning);
                  }
                 );
        }

        [Fact]
        public void SidebarReducedMotionImmediatelyAppliesHoverWithoutStartingTimer()
        {
            OnSta(() =>
                  {
                      using HoverListBox list = SidebarList(false);
                      HoverSidebar(list, 0);
                      Assert.Equal(1, list.HoverAmount(0));
                      HoverSidebar(list, 1);
                      Assert.Equal(0, list.HoverAmount(0));
                      Assert.Equal(1, list.HoverAmount(1));
                      Assert.False(SidebarMotion(list).IsRunning);
                      InvokeSidebar(list, "OnMouseLeave", EventArgs.Empty);
                      Assert.Equal(0, list.HoverAmount(1));
                  }
                 );
        }

        [Fact]
        public void SidebarSnapshotsStillPaintScrolledItemsAndClearAnEmptyList()
        {
            OnSta(() =>
                  {
                      using Form form = new Form();
                      using HoverListBox list = SidebarList();
                      form.Controls.Add(list);
                      for (int index = 3; index < 30; index++)
                      {
                          list.Items.Add($"Network {index}");
                      }
                      list.DrawItem += (_, e) =>
                      {
                          if (e.Index < 0)
                              return;

                          using SolidBrush brush = new SolidBrush(Color.FromArgb(20 + e.Index, 80, 120));
                          e.Graphics.FillRectangle(brush, e.Bounds);
                      };
                      form.Show();
                      Application.DoEvents();
                      list.SelectedIndex = 22;
                      list.TopIndex = 20;
                      using Bitmap image = new Bitmap(list.Width, list.Height);
                      list.DrawToBitmap(image, list.ClientRectangle);
                      Assert.Equal(Color.FromArgb(40, 80, 120).ToArgb(), image.GetPixel(10, 10).ToArgb());
                      Assert.Equal(22, list.SelectedIndex);
                      Assert.Equal(AccessibleRole.List, list.AccessibilityObject.Role);
                      Assert.Equal(30, list.AccessibilityObject.GetChildCount());
                      list.Items.Clear();
                      list.DrawToBitmap(image, list.ClientRectangle);
                      Assert.Equal(list.BackColor.ToArgb(), image.GetPixel(10, 10).ToArgb());
                      Assert.Equal(list.BackColor.ToArgb(), image.GetPixel(10, list.Height - 10).ToArgb());
                  }
                 );
        }

        [Fact]
        public void SidebarHoverLeavesTheActiveComputerDraftAndEditorInstancesUntouched()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(3);
                      using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets);
                      form.Show();
                      Application.DoEvents();
                      HoverListBox list = Find<HoverListBox>(form, "Networks");
                      DataGridView grid = Find<DataGridView>(form, "NetworkComputers");
                      EditName(grid, 0, "Draft survives hover");
                      Control editor = grid.EditingControl!;
                      DataGridViewRow row = grid.Rows[0];
                      int changes = 0;
                      list.SelectedIndexChanged += (_, _) => changes++;
                      for (int index = 0; index < 100; index++)
                      {
                          HoverSidebar(list, index % 2);
                          SidebarMotion(list).Advance(.25F);
                      }
                      SidebarMotion(list).Finish();
                      Assert.Equal(0, changes);
                      Assert.Same(row, grid.Rows[0]);
                      Assert.Same(editor, grid.EditingControl);
                      Assert.Equal("Draft survives hover", editor.Text);
                  }
                 );
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        public void SidebarBufferKeepsNativeTextVisibleOnEveryRowAfterScrolling(int topIndex)
        {
            OnSta(() =>
                  {
                      List<ServerProfile> profiles = Enumerable.Range(0, 24)
                          .Select(index => new ServerProfile
                          {
                              Id = $"test-{index}",
                              Name = $"Visible network {index:00}",
                              ServerAddress = "example.invalid"
                          })
                          .ToList();
                      using ProfilesForm form = new ProfilesForm(profiles);
                      form.Show();
                      Application.DoEvents();
                      HoverListBox list = Find<HoverListBox>(form, "Networks");
                      list.TopIndex = topIndex;
                      using Bitmap image = new Bitmap(list.Width, list.Height);
                      list.DrawToBitmap(image, list.ClientRectangle);
                      for (int index = topIndex; index < list.Items.Count; index++)
                      {
                          Rectangle bounds = list.GetItemRectangle(index);
                          if (!list.ClientRectangle.Contains(bounds))
                              break;

                          int textPixels = 0;
                          for (int y = bounds.Top; y < bounds.Bottom; y++)
                          {
                              for (int x = bounds.Left; x < bounds.Right; x++)
                              {
                                  Color pixel = image.GetPixel(x, y);
                                  if (pixel.R < 100 && pixel.G < 120 && pixel.B < 140)
                                      textPixels++;
                              }
                          }
                          Assert.True(textPixels > 10, $"Network row {index} lost its text in the buffer.");
                      }
                  }
                 );
        }
        #endregion

        #region Methods
        private static HoverListBox SidebarList(bool motionEnabled = true)
        {
            HoverListBox list = new HoverListBox(() => motionEnabled)
            {
                Size = new Size(180, 200),
                DrawMode = DrawMode.OwnerDrawFixed,
                BorderStyle = BorderStyle.None,
                IntegralHeight = false,
                ItemHeight = 36,
                BackColor = AppTheme.canvas
            };
            list.Items.AddRange(new object[] { "Public", "Studio", "Home" });
            _ = list.Handle;
            return list;
        }

        private static MotionTween SidebarMotion(HoverListBox list) =>
            (MotionTween)typeof(HoverListBox).GetField("_motion", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(list)!;

        private static void HoverSidebar(HoverListBox list, int index)
        {
            Rectangle bounds = list.GetItemRectangle(index);
            MouseEventArgs args = new MouseEventArgs(MouseButtons.None, 0, bounds.Left + 10, bounds.Top + 10, 0);
            InvokeSidebar(list, "OnMouseMove", args);
        }

        private static void InvokeSidebar(HoverListBox list, string method, object args) =>
            typeof(HoverListBox).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(list, new[] { args });
        #endregion

        #region Nested Types
        private sealed class SidebarPaintObserver : NativeWindow, IDisposable
        {
            #region Constants and Fields
            private const int WM_ERASEBKGND = 0x0014;
            #endregion

            #region Constructors and Destructors
            internal SidebarPaintObserver(Control control) => AssignHandle(control.Handle);
            #endregion

            #region Properties and Indexers
            internal int BackgroundErases { get; private set; }
            #endregion

            #region Methods
            public void Dispose() => ReleaseHandle();

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_ERASEBKGND)
                    BackgroundErases++;
                base.WndProc(ref m);
            }
            #endregion
        }
        #endregion
    }
}