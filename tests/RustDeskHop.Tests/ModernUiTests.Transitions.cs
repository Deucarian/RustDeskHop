using System.Drawing;
using System.Windows.Forms;
using Xunit;

namespace RustDeskHop.Tests
{
    public sealed partial class ModernUiTests
    {
        #region Test Methods
        [Fact]
        public void NetworkListReflowKeepsItemIdentityAndItsBackground()
        {
            OnSta(() =>
                  {
                      using Form host = new Form();
                      using ListBox list = new ListBox { Dock = DockStyle.Fill, BackColor = AppTheme.canvas };
                      host.Controls.Add(list);
                      ServerProfile first = new ServerProfile { Name = "First" };
                      ServerProfile second = new ServerProfile { Name = "Second" };
                      list.Items.AddRange([first, second]);
                      host.Show();
                      Application.DoEvents();
                      using TransitionFrame before = TransitionFrame.Capture(list);
                      list.Items.RemoveAt(0);
                      using TransitionFrame after = TransitionFrame.Capture(list);
                      TransitionRow oldRow = Assert.Single(before.Rows, row => row.Key == second);
                      TransitionRow newRow = Assert.Single(after.Rows);
                      Assert.Same(second, newRow.Key);
                      Assert.True(newRow.Bounds.Top < oldRow.Bounds.Top);
                      Assert.Equal(AppTheme.canvas, after.Background);
                  }
                 );
        }

        [Fact]
        public void InlineAddKeepsKeyboardFocusDuringAndAfterReflow()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(1);
                      using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets);
                      form.Show();
                      form.Activate();
                      Application.DoEvents();
                      ClickAddRow(form);
                      Application.DoEvents();
                      DataGridView grid = Find<DataGridView>(form, "NetworkComputers");
                      TextBox editor = Assert.IsAssignableFrom<TextBox>(grid.EditingControl);
                      Assert.True(editor.ContainsFocus);
                      editor.Text = "Draft stays focused";
                      foreach (TransitionOverlay overlay in Descendants(form).OfType<TransitionOverlay>().ToArray())
                      {
                          Assert.Equal(AccessibleStates.Invisible | AccessibleStates.Offscreen,
                                       overlay.AccessibilityObject.State
                                      );
                          Assert.Equal(0, overlay.AccessibilityObject.GetChildCount());
                          overlay.Finish();
                      }
                      Assert.True(editor.ContainsFocus);
                      Assert.Equal("Draft stays focused", editor.Text);
                      Assert.Single(settings.Targets);
                  }
                 );
        }

        [Fact]
        public void ReflowUsesStableRowIdentityAndIntermediatePositions()
        {
            OnSta(() =>
                  {
                      using Form host = new Form();
                      using DataGridView grid = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false };
                      host.Controls.Add(grid);
                      grid.Columns.Add("Name", "Name");
                      object first = new object();
                      object second = new object();
                      grid.Rows[grid.Rows.Add("First")].Tag = first;
                      grid.Rows[grid.Rows.Add("Second")].Tag = second;
                      host.Show();
                      Application.DoEvents();
                      using TransitionFrame before = TransitionFrame.Capture(grid);
                      grid.Rows.RemoveAt(0);
                      using TransitionFrame after = TransitionFrame.Capture(grid);
                      TransitionRow oldRow = Assert.Single(before.Rows, row => row.Key == second);
                      TransitionRow newRow = Assert.Single(after.Rows, row => row.Key == second);
                      Rectangle midway = TransitionOverlay.ReflowBounds(oldRow.Bounds, newRow.Bounds, .5F);
                      Assert.InRange(midway.Y, newRow.Bounds.Y + 1, oldRow.Bounds.Y - 1);
                      Assert.Equal(newRow.Bounds, TransitionOverlay.ReflowBounds(oldRow.Bounds, newRow.Bounds, 1));
                      Assert.Same(second, newRow.Key);
                  }
                 );
        }

        [Fact]
        public void ReplacingOrCancellingTransitionsDisposesOldOverlays()
        {
            OnSta(() =>
                  {
                      using Form host = new Form();
                      using Panel content = new Panel { Bounds = new Rectangle(0, 0, 200, 100), BackColor = Color.White };
                      host.Controls.Add(content);
                      host.Show();
                      using ContentTransition transition = new ContentTransition(content, () => true);
                      transition.Begin();
                      content.BackColor = Color.Blue;
                      transition.End();
                      Application.DoEvents();
                      TransitionOverlay first = Assert.Single(host.Controls.OfType<TransitionOverlay>());
                      transition.Begin();
                      Assert.True(first.IsDisposed);
                      content.BackColor = Color.White;
                      transition.End();
                      Application.DoEvents();
                      Assert.Single(host.Controls.OfType<TransitionOverlay>());
                      transition.Cancel();
                      Assert.Empty(host.Controls.OfType<TransitionOverlay>());
                      transition.Begin();
                      transition.End();
                      transition.Dispose();
                      Application.DoEvents();
                      Assert.Empty(host.Controls.OfType<TransitionOverlay>());
                  }
                 );
        }

        [Fact]
        public void ReducedMotionDoesNotCreateAnOverlay()
        {
            OnSta(() =>
                  {
                      using Form host = new Form();
                      using Panel content = new Panel { Dock = DockStyle.Fill };
                      host.Controls.Add(content);
                      host.Show();
                      using ContentTransition transition = new ContentTransition(content, () => false);
                      transition.Begin();
                      content.BackColor = Color.Blue;
                      transition.End();
                      Application.DoEvents();
                      Assert.Empty(host.Controls.OfType<TransitionOverlay>());
                      Assert.Equal(Color.Blue, content.BackColor);
                  }
                 );
        }
        #endregion
    }
}