using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Xunit;

namespace RustDeskHop.Tests
{
    public sealed partial class ModernUiTests
    {
        #region Test Methods
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WindowSizeAndMaximizedStateSurviveANewApplicationInstance(bool maximized)
        {
            OnSta(() =>
            {
                using TestDirectory directory = new TestDirectory();
                string path = directory.FilePath("window.json");
                Size chosen = new Size(1000, 600);
                using (MainForm first = CreatePlacementForm(new WindowPlacementStore(path)))
                {
                    first.Show();
                    Application.DoEvents();
                    first.ClientSize = chosen;
                    if (maximized)
                        first.WindowState = FormWindowState.Maximized;
                    Application.DoEvents();
                    first.Close();
                }
                using MainForm second = CreatePlacementForm(new WindowPlacementStore(path));
                second.Show();
                Application.DoEvents();
                Assert.Equal(maximized ? FormWindowState.Maximized : FormWindowState.Normal, second.WindowState);
                second.WindowState = FormWindowState.Normal;
                Application.DoEvents();
                Assert.Equal(chosen, second.ClientSize);
            });
        }

        [Theory]
        [InlineData("missing")]
        [InlineData("{broken")]
        [InlineData("{\"ClientWidth\":-1,\"ClientHeight\":600}")]
        [InlineData("null")]
        public void UnusableWindowPreferencesFallBackToOriginalSize(string contents)
        {
            OnSta(() =>
            {
                using TestDirectory directory = new TestDirectory();
                string path = directory.FilePath("window.json");
                if (contents != "missing")
                    File.WriteAllText(path, contents);
                using MainForm form = CreatePlacementForm(new WindowPlacementStore(path));
                form.Show();
                Application.DoEvents();
                Assert.Equal(new Size(810, 270), form.ClientSize);
                Assert.Equal(FormWindowState.Normal, form.WindowState);
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FullscreenAndMinimizeDoNotReplaceSavedNormalWindowSize(bool fullscreen)
        {
            OnSta(() =>
            {
                using TestDirectory directory = new TestDirectory();
                string path = directory.FilePath("window.json");
                Size chosen = new Size(1000, 600);
                using (MainForm first = CreatePlacementForm(new WindowPlacementStore(path)))
                {
                    first.Show();
                    first.ClientSize = chosen;
                    if (fullscreen)
                        Find<ModernButton>(first, "Fullscreen").PerformClick();
                    else
                        first.WindowState = FormWindowState.Minimized;
                    Application.DoEvents();
                    first.Close();
                }
                using MainForm second = CreatePlacementForm(new WindowPlacementStore(path));
                second.Show();
                Application.DoEvents();
                Assert.Equal(chosen, second.ClientSize);
                Assert.Equal(FormWindowState.Normal, second.WindowState);
                Assert.Equal(FormBorderStyle.Sizable, second.FormBorderStyle);
            });
        }

        [Fact]
        public void OversizedSavedWindowFitsTheCurrentMonitor()
        {
            OnSta(() =>
            {
                using TestDirectory directory = new TestDirectory();
                WindowPlacementStore store = new WindowPlacementStore(directory.FilePath("window.json"));
                store.Save(new WindowPlacement(1000000, 1000000, false));
                using MainForm form = CreatePlacementForm(store);
                form.Show();
                Application.DoEvents();
                Assert.True(Screen.FromControl(form).WorkingArea.Contains(form.Bounds));
            });
        }

        [Fact]
        public void ResizeTimerPersistsWithoutExitingOrChangingComputerSettings()
        {
            OnSta(() =>
            {
                using TestDirectory directory = new TestDirectory();
                string path = directory.FilePath("window.json");
                using MainForm form = CreatePlacementForm(new WindowPlacementStore(path));
                form.Show();
                form.ClientSize = new Size(1000, 600);
                System.Windows.Forms.Timer timer = (System.Windows.Forms.Timer)typeof(MainForm)
                    .GetField("_placementTimer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(form)!;
                typeof(System.Windows.Forms.Timer).GetMethod("OnTick", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(timer, [EventArgs.Empty]);
                Assert.Equal(1000, new WindowPlacementStore(path).Load()!.ClientWidth);
                Assert.Single(Directory.GetFiles(Path.GetDirectoryName(path)!));
            });
        }
        #endregion

        #region Methods
        private static MainForm CreatePlacementForm(IWindowPlacementStore store)
        {
            ConnectionTestRig rig = new ConnectionTestRig();
            return new MainForm(new MemorySettingsStore(Settings(3)), rig.Workflow, _ => rig.Interaction,
                                windowPlacementStore: store);
        }
        #endregion
    }
}
