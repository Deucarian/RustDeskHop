using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Xunit;

namespace RustDeskHop.Tests
{
    public sealed class NativeWindowTests
    {
        #region Constants and Fields
        private const int CAPTION = 0x00C00000, SYSTEM_MENU = 0x00080000;
        private const int RESIZE_FRAME = 0x00040000, MINIMIZE = 0x00020000, MAXIMIZE = 0x00010000;
        #endregion

        #region Test Methods
        [Fact]
        public void AllWindowsUseRealCaptionsAndNativeWindowCapabilities()
        {
            OnSta(() =>
                  {
                      ServerProfile[] profiles = new[]
                      {
                          new ServerProfile
                          {
                              Name = "Public",
                              ServerAddress = "public"
                          }
                      };
                      using MainForm main =
                          TestApplication.CreateMainForm(new AppSettings { Profiles = profiles.ToList() });
                      using ProfilesForm networks = new ProfilesForm(profiles);
                      using PublicSignInForm signIn =
                          TestApplication.CreateSignInForm("not-launched-during-this-test.exe");

                      // Do not show signIn: its Shown event launches RustDesk.
                      foreach (Form? form in new Form[]
                               {
                                   main,
                                   networks,
                                   signIn
                               })
                      {
                          int style = GetWindowLong(form.Handle, -16);
                          Assert.Equal(CAPTION | SYSTEM_MENU, style & (CAPTION | SYSTEM_MENU));
                          Assert.Equal(form.MinimizeBox, (style & MINIMIZE) != 0);
                          Assert.Equal(form.MaximizeBox, (style & MAXIMIZE) != 0);
                          Assert.Equal(form.FormBorderStyle == FormBorderStyle.Sizable, (style & RESIZE_FRAME) != 0);
                          Assert.Same(AppBranding.Icon, form.Icon);
                          Assert.True(form.Height - form.ClientSize.Height >= SystemInformation.CaptionHeight);
                          Panel content = Assert.IsType<Panel>(Assert.Single(form.Controls.Cast<Control>()));
                          Assert.Equal("WindowFrame", content.Name);
                          Assert.Single(content.Controls.Find("WindowContent", true));
                          Assert.Equal(form.ClientRectangle, content.Bounds);
                          Assert.Equal(Padding.Empty, form.Padding);
                      }
                  }
                 );
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DwmAcceptsNativeCaptionThemeForBothActivationStates(bool active)
        {
            OnSta(() =>
                  {
                      if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
                          return;

                      using MainForm form = TestApplication.CreateMainForm(new AppSettings());

                      // Caption/text colours are set-only DWM attributes, not queryable attributes.
                      Assert.True(NativeWindowTheme.Apply(form.Handle, active));
                  }
                 );
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
            Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "Native shell test timed out.");
            if (failure is not null)
                ExceptionDispatchInfo.Capture(failure).Throw();
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
        private static extern int GetWindowLong(IntPtr window, int index);
        #endregion
    }
}