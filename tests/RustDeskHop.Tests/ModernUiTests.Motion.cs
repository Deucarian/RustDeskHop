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
        public void MotionInterpolatesRetargetsAndStopsItsTimer()
        {
            OnSta(() =>
                  {
                      float painted = -1;
                      using MotionTween motion = new MotionTween(value => painted = value, () => true);
                      motion.To(1);
                      Assert.True(motion.IsRunning);
                      motion.Advance(.25F);
                      Assert.InRange(painted, .1F, .99F);
                      float midway = painted;
                      motion.To(0);
                      Assert.Equal(midway, painted);
                      motion.Advance(.5F);
                      Assert.InRange(painted, 0, midway);
                      motion.Finish();
                      Assert.Equal(0, painted);
                      Assert.False(motion.IsRunning);
                  }
                 );
        }

        [Fact]
        public void ReducedMotionIsImmediateAndCanChangeDuringATransition()
        {
            OnSta(() =>
                  {
                      bool enabled = false;
                      using MotionTween motion = new MotionTween(_ => { }, () => enabled);
                      motion.To(1);
                      Assert.Equal(1, motion.Value);
                      Assert.False(motion.IsRunning);
                      enabled = true;
                      motion.To(0);
                      Assert.True(motion.IsRunning);
                      enabled = false;
                      motion.Tick();
                      Assert.Equal(0, motion.Value);
                      Assert.False(motion.IsRunning);
                  }
                 );
        }

        [Fact]
        public void TabsUseTheSameOutlinePaletteAsTestActions()
        {
            OnSta(() =>
                  {
                      using ProfilesForm form = new ProfilesForm(Settings(1).Profiles);
                      form.Show();
                      Application.DoEvents();
                      SectionTabs tabs = Find<SectionTabs>(form, "NetworkSections");
                      foreach (ModernButton button in Descendants(tabs).OfType<ModernButton>())
                      {
                          Assert.True(button.Accent);
                          Assert.False(button.Quiet);
                          Assert.False(button.Primary);
                          Assert.Equal(UiMotion.ButtonFill(false, true, 0, button.SelectedTab).ToArgb(),
                                       (button.SelectedTab ? AppTheme.selection : Color.White).ToArgb()
                                      );
                      }
                      Assert.Single(Descendants(tabs).OfType<ModernButton>(), button => button.SelectedTab);
                  }
                 );
        }

        [Fact]
        public void EveryCompanionWindowHasAnExactNonResizableSize()
        {
            OnSta(() =>
                  {
                      using MainForm main = TestApplication.CreateMainForm(Settings(3));
                      using ProfilesForm manager = new ProfilesForm(Settings(3).Profiles);
                      using PublicSignInForm signIn = TestApplication.CreateSignInForm("fake-client.exe");
                      foreach (BrandedForm form in new BrandedForm[] { main, manager, signIn })
                      {
                          form.Show();
                          Application.DoEvents();
                          Size original = form.Size;
                          Assert.False(form.MaximizeBox);
                          Assert.NotEqual(FormBorderStyle.Sizable, form.FormBorderStyle);
                          Assert.Equal(original, form.MinimumSize);
                          Assert.Equal(original, form.MaximumSize);
                          form.Size = new Size(100, 100);
                          Assert.Equal(original, form.Size);
                          form.Size = new Size(1920, 1080);
                          Assert.Equal(original, form.Size);
                          Assert.Equal(90, form.ScaleState.Percent);
                          Assert.Empty(Descendants(form).OfType<TrackBar>());
                          form.Hide();
                      }
                  }
                 );
        }

        [Fact]
        public void RapidPageChangesKeepDraftsAndCorrectSelection()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(3);
                      using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets);
                      form.Show();
                      Application.DoEvents();
                      SectionTabs tabs = Find<SectionTabs>(form, "NetworkSections");
                      DataGridView grid = Find<DataGridView>(form, "NetworkComputers");
                      EditName(grid, 0, "Unfinished rename");
                      for (int index = 0; index < 20; index++)
                      {
                          tabs.SelectedIndex = index % 2;
                          Application.DoEvents();
                      }
                      tabs.Transition.Cancel();
                      Assert.Equal(1, tabs.SelectedIndex);
                      Assert.Equal("Unfinished rename", Assert.IsAssignableFrom<TextBox>(grid.EditingControl).Text);
                      Assert.Equal("Computer 1", settings.Targets[0].Name);
                      Assert.Empty(Descendants(form).OfType<TransitionOverlay>());
                      Find<ModernButton>(form, "SaveNetwork").PerformClick();
                      Assert.Equal("Unfinished rename", form.Targets[0].Name);
                  }
                 );
        }

        [Fact]
        public void HoverFeedbackCoversGridRowActionsAndEditableFields()
        {
            OnSta(() =>
                  {
                      using ProfilesForm form = new ProfilesForm(Settings(3).Profiles,
                                                                 Settings(3).Targets,
                                                                 (_, _, _) => Task.FromResult(ConnectionOutcome.STARTED)
                                                                );
                      form.Show();
                      Application.DoEvents();
                      RowActionGrid grid = Find<RowActionGrid>(form, "NetworkComputers");
                      Rectangle bounds = grid.GetCellDisplayRectangle(2, 0, false);
                      MouseEventArgs hover = new MouseEventArgs(MouseButtons.None,
                                                                0,
                                                                bounds.X + bounds.Width / 2,
                                                                bounds.Y + bounds.Height / 2,
                                                                0
                                                               );
                      typeof(Control).GetMethod("OnMouseMove", BindingFlags.Instance | BindingFlags.NonPublic)!
                          .Invoke(grid, [hover]);
                      Assert.Equal(Cursors.Hand, grid.Cursor);
                      // Exercise a deliberate move onto an editable cell rather than depending on
                      // the desktop cursor's position outside the grid after a synthetic MouseLeave.
                      Rectangle name = grid.GetCellDisplayRectangle(0, 0, false);
                      MouseEventArgs editable = new MouseEventArgs(MouseButtons.None,
                                                                   0,
                                                                   name.X + name.Width / 2,
                                                                   name.Y + name.Height / 2,
                                                                   0
                                                                  );
                      typeof(Control).GetMethod("OnMouseMove", BindingFlags.Instance | BindingFlags.NonPublic)!
                          .Invoke(grid, [editable]);
                      Assert.Equal(Cursors.Default, grid.Cursor);
                  }
                 );
        }
        #endregion
    }
}