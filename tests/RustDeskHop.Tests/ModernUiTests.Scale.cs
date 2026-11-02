using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;
using Xunit;

namespace RustDeskHop.Tests
{
    public sealed partial class ModernUiTests
    {
        #region Test Methods
        [Theory]
        [InlineData(50)]
        [InlineData(75)]
        [InlineData(100)]
        [InlineData(125)]
        [InlineData(150)]
        public void FixedScaleIgnoresTheOldPreferenceAndHasNoSlider(int percent)
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(3);
                      string json = JsonSerializer.Serialize(settings);
                      settings = JsonSerializer.Deserialize<AppSettings>(
                          "{\"UiScalePercent\":" + percent + "," + json[1..])!;
                      using MainForm form = TestApplication.CreateMainForm(settings);
                      form.Show();
                      Application.DoEvents();
                      ComputerGrid grid = Find<ComputerGrid>(form, "Computers");
                      int minimumRow = (int)Math.Round(64 * .9F);
                      Assert.InRange(grid.Rows[0].Height, minimumRow, minimumRow + 4);
                      Assert.Equal(AppTheme.body.Size * .9F, grid.Font.Size, 2);
                      Assert.Equal(90, form.ScaleState.Percent);
                      Assert.Empty(Descendants(form).OfType<TrackBar>());
                      Assert.Equal(new Size(810, 270), form.ClientSize);
                      Assert.DoesNotContain("UiScalePercent", JsonSerializer.Serialize(settings));
                      Assert.True(grid.Bottom <= Find<ModernButton>(form, "ManageNetworks").Top);
                  }
                 );
        }

        [Fact]
        public void ScalingNetworkPanesRepeatedlyPreservesDraftsAndToggleState()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(3);
                      using UiScaleState scale = new UiScaleState(75);
                      using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets);
                      form.UseScaleState(scale);
                      form.Show();
                      Application.DoEvents();
                      Assert.Empty(Descendants(form).OfType<SplitContainer>());
                      SectionTabs sections = Find<SectionTabs>(form, "NetworkSections");
                      DataGridView grid = Find<DataGridView>(form, "NetworkComputers");
                      EditName(grid, 0, "Unsaved name");
                      for (int cycle = 0; cycle < 3; cycle++)
                      {
                          foreach (int percent in new[] { 50, 150, 75, 100, 125, 75 })
                          {
                              scale.SetPercent(percent);
                              Application.DoEvents();
                              Padding padding = grid.Rows[0].Cells[0].InheritedStyle.Padding;
                              Assert.True(grid.Columns[0].Width > padding.Horizontal,
                                          $"{percent}%: width={grid.Columns[0].Width}; padding={padding}"
                                         );
                              Assert.Equal("Unsaved name", Assert.IsAssignableFrom<TextBox>(grid.EditingControl).Text);
                              Assert.Equal("Computer 1", settings.Targets[0].Name);
                              Assert.Equal(2, grid.Rows.Count - 1);
                              ModernButton save = Find<ModernButton>(form, "SaveNetwork");
                              Assert.True(save.Parent!.ClientRectangle.Contains(save.Bounds));
                          }
                      }
                      grid.EndEdit();
                      for (int index = 0; index < 2; index++)
                      {
                          sections.SelectedIndex = index;
                          Application.DoEvents();
                          ModernButton[] tabs = Descendants(sections).OfType<ModernButton>().ToArray();
                          Assert.Single(tabs, tab => tab.SelectedTab);
                          Assert.Single(tabs, tab => tab.TabStop);
                          Assert.All(tabs, tab => Assert.Equal(tab.SelectedTab,
                                                              tab.AccessibilityObject.State
                                                                  .HasFlag(AccessibleStates.Selected)
                                                             )
                                    );
                          Assert.Equal(tabs[0].Font.Size, tabs[1].Font.Size);
                          Assert.Equal(tabs[0].Height, tabs[1].Height);
                      }
                      sections.SelectedIndex = 1;
                      Find<ModernButton>(form, "SaveNetwork").PerformClick();
                      Assert.Equal("Unsaved name", form.Targets[0].Name);
                  }
                 );
        }

        [Fact]
        public void NetworkFieldsShrinkAfterGrowingWithoutHidingLowerFields()
        {
            OnSta(() =>
                  {
                      AppSettings settings = Settings(1);
                      using UiScaleState scale = new UiScaleState(75);
                      using ProfilesForm form = new ProfilesForm(settings.Profiles, settings.Targets);
                      form.UseScaleState(scale);
                      form.Show();
                      Application.DoEvents();
                      SectionTabs sections = Find<SectionTabs>(form, "NetworkSections");
                      sections.SelectedIndex = 0;
                      Application.DoEvents();
                      foreach (int percent in new[] { 75, 150, 50, 100, 75, 150, 50, 75 })
                      {
                          scale.SetPercent(percent);
                          Application.DoEvents();
                          foreach (InputSurface field in Descendants(sections.SelectedTab!).OfType<InputSurface>())
                          {
                              int expected = (int)Math.Round(UiMetrics.BUTTON_HEIGHT * percent / 100F);
                              Assert.InRange(field.Height, expected - 1, expected + 1);
                              Assert.True(field.Parent!.ClientRectangle.Contains(field.Bounds));
                          }
                          Label hint = Descendants(sections.SelectedTab!).OfType<Label>()
                              .Single(label => label.Text.StartsWith("Use “public”"));
                          Assert.True(hint.Parent!.ClientRectangle.Contains(hint.Bounds),
                                      $"{percent}%: hint={hint.Bounds}; parent={hint.Parent.ClientRectangle}"
                                     );
                      }
                  }
                 );
        }

        [Fact]
        public void FixedScaleDoesNotRewriteLegacySettingsAtStartup()
        {
            OnSta(() =>
                  {
                      using TestDirectory temporary = new TestDirectory();
                      string path = temporary.FilePath("settings.json");
                      AppSettings settings = Settings(3);
                      ConfigStore.Save(settings, path);
                      RecordingScaleStore store = new RecordingScaleStore(path);
                      ConnectionTestRig rig = new ConnectionTestRig();
                      using MainForm form = new MainForm(store, rig.Workflow, _ => rig.Interaction);
                      form.Show();
                      Application.DoEvents();
                      Assert.Equal(0, store.SaveCount);
                      Assert.Equal(90, form.ScaleState.Percent);
                      AppSettings loaded = ConfigStore.Load(path, null, out _);
                      Assert.Equal(JsonSerializer.Serialize(settings.Targets),
                                   JsonSerializer.Serialize(loaded.Targets)
                                  );
                      Assert.Equal(JsonSerializer.Serialize(settings.Profiles),
                                   JsonSerializer.Serialize(loaded.Profiles)
                                  );
                  }
                 );
        }
        #endregion

        #region Nested Types
        private sealed class RecordingScaleStore(string path) : ISettingsStore
        {
            #region Properties and Indexers
            internal int SaveCount { get; private set; }
            #endregion

            #region Methods
            public AppSettings Load(out string? warning) => ConfigStore.Load(path, null, out warning);
            public void Save(AppSettings settings)
            {
                ConfigStore.Save(settings, path);
                SaveCount++;
            }
            #endregion
        }
        #endregion
    }
}