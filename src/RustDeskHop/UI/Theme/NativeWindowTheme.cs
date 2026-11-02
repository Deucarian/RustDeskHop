using System.Runtime.InteropServices;

namespace RustDeskHop.UI.Theme
{
    // Presentation only: never replace the native frame or intercept window commands.
    internal static class NativeWindowTheme
    {
        #region Constants and Fields
        private const int DEFAULT_COLOR = -1;
        private const int USE_IMMERSIVE_DARK_MODE = 20, CORNER_PREFERENCE = 33;
        private const int BORDER_COLOR = 34, CAPTION_COLOR = 35, TEXT_COLOR = 36;
        #endregion

        #region Methods
        internal static bool Apply(IntPtr window, bool active)
        {
            // Earlier Windows versions retain their complete, standard system frame.
            if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
                return false;

            bool highContrast = SystemInformation.HighContrast;
            bool applied = Set(window, USE_IMMERSIVE_DARK_MODE, 0);
            applied &= Set(window, CORNER_PREFERENCE, highContrast ? 0 : 2);
            applied &= Set(window,
                           CAPTION_COLOR,
                           highContrast ? DEFAULT_COLOR : ColorTranslator.ToWin32(AppTheme.canvas)
                          );
            applied &= Set(window,
                           TEXT_COLOR,
                           highContrast
                               ? DEFAULT_COLOR
                               : ColorTranslator.ToWin32(active ? AppTheme.ink : AppTheme.muted)
                          );
            applied &= Set(window,
                           BORDER_COLOR,
                           highContrast
                               ? DEFAULT_COLOR
                               : ColorTranslator.ToWin32(active ? AppTheme.windowBorder : AppTheme.line)
                          );
            return applied;
        }

        private static bool Set(IntPtr window, int attribute, int value)
        {
            // Styling is optional; if DWM rejects an attribute, native behaviour remains.
            return DwmSetWindowAttribute(window, attribute, ref value, sizeof(int)) >= 0;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
        #endregion
    }
}