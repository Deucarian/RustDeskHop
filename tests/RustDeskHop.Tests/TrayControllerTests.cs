using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using Xunit;

namespace RustDeskHop.Tests
{
    public sealed class TrayControllerTests
    {
        #region Test Methods
        [Fact]
        public void CloseHidesAndOpenRestoresTheSameWindow()
        {
            OnSta(() =>
                  {
                      using Form window = new Form();
                      using TrayController tray = new TrayController(window, showNotifications: false);
                      window.Show();
                      nint handle = window.Handle;
                      Assert.True(tray.NotificationIcon.Visible);
                      Assert.Same(AppBranding.Icon, tray.NotificationIcon.Icon);
                      window.Close();
                      Assert.False(window.Visible);
                      Assert.False(window.IsDisposed);
                      Assert.True(tray.NotificationIcon.Visible);
                      tray.Menu.Items[0].PerformClick();
                      Assert.True(window.Visible);
                      Assert.Equal(handle, window.Handle);
                      Assert.Equal(FormWindowState.Normal, window.WindowState);
                  }
                 );
        }

        [Theory]
        [InlineData(FormWindowState.Normal)]
        [InlineData(FormWindowState.Maximized)]
        public void MinimizeHidesAndRestoresPreviousState(FormWindowState state)
        {
            OnSta(() =>
                  {
                      using Form window = new Form();
                      using TrayController tray = new TrayController(window, showNotifications: false);
                      window.Show();
                      window.WindowState = state;
                      window.WindowState = FormWindowState.Minimized;
                      Assert.False(window.Visible);
                      tray.OpenWindow();
                      Assert.True(window.Visible);
                      Assert.Equal(state, window.WindowState);
                  }
                 );
        }

        [Fact]
        public void TrayExitDisposesWindowAndIconButKeepsSharedBrandingAlive()
        {
            OnSta(() =>
                  {
                      using Form window = new Form();
                      using TrayController tray = new TrayController(window, showNotifications: false);
                      window.Show();
                      window.Close();
                      tray.Menu.Items[2].PerformClick();
                      Assert.True(window.IsDisposed);
                      Assert.False(tray.NotificationIcon.Visible);
                      Assert.True(tray.Menu.IsDisposed);
                      Assert.NotEqual(IntPtr.Zero, AppBranding.Icon.Handle);
                      tray.Dispose();
                  }
                 );
        }

        [Fact]
        public void ExitHonorsAnExistingCloseVeto()
        {
            OnSta(() =>
                  {
                      using Form window = new Form();
                      using TrayController tray = new TrayController(window, showNotifications: false);
                      window.FormClosing += (_, e) => e.Cancel = true;
                      window.Show();
                      tray.Exit();
                      Assert.True(window.Visible);
                      Assert.False(window.IsDisposed);
                      Assert.True(tray.NotificationIcon.Visible);
                      window.Close();
                      Assert.False(window.Visible);
                  }
                 );
        }

        [Fact]
        public void OpenFocusesOwnedDialogAndExitDoesNotDiscardIt()
        {
            OnSta(() =>
                  {
                      using Form window = new Form();
                      using Form dialog = new Form();
                      using TrayController tray = new TrayController(window, showNotifications: false);
                      window.Show();
                      dialog.Show(window);
                      window.Enabled = false; // Model a modal editor without blocking the test.
                      tray.Exit();
                      Assert.False(window.IsDisposed);
                      Assert.True(dialog.Visible);
                      Assert.True(dialog.ContainsFocus);
                      window.Enabled = true;
                      dialog.Close();
                      tray.Exit();
                      Assert.True(window.IsDisposed);
                  }
                 );
        }

        [Fact]
        public void OnlyUserCloseIsConvertedToTrayHide()
        {
            foreach (CloseReason reason in Enum.GetValues<CloseReason>())
            {
                Assert.Equal(reason == CloseReason.UserClosing, TrayController.ShouldHideOnClose(reason));
            }
        }
        #endregion

        #region Methods
        private static void OnSta(Action action)
        {
            Exception? failure = null;
            Thread thread = new Thread(() =>
                                       {
                                           try
                                           {
                                               action();
                                           }
                                           catch (Exception error)
                                           {
                                               failure = error;
                                           }
                                       }
                                      );
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
            Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "Tray test timed out.");
            if (failure is not null)
                ExceptionDispatchInfo.Capture(failure).Throw();
        }
        #endregion
    }
}