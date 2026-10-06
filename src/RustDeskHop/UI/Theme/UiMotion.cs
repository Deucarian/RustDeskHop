using System.Runtime.InteropServices;

namespace RustDeskHop.UI.Theme
{
    internal static class UiMotion
    {
        #region Constants and Fields
        internal const int HOVER_DURATION = 120;
        internal const int CONTENT_DURATION = 180;
        private const uint GET_CLIENT_AREA_ANIMATION = 0x1042;
        #endregion

        #region Properties and Indexers
        internal static bool Enabled => !SystemInformation.HighContrast
            && SystemParametersInfo(GET_CLIENT_AREA_ANIMATION, 0, out bool enabled, 0) && enabled;
        #endregion

        #region Methods
        internal static float Ease(float progress) => 1 - MathF.Pow(1 - Math.Clamp(progress, 0, 1), 3);

        internal static Color Blend(Color from, Color to, float progress)
        {
            float amount = Math.Clamp(progress, 0, 1);
            int Channel(int start, int end) => (int)Math.Round(start + (end - start) * amount);
            return Color.FromArgb(Channel(from.A, to.A),
                                  Channel(from.R, to.R),
                                  Channel(from.G, to.G),
                                  Channel(from.B, to.B)
                                 );
        }

        internal static Color ButtonFill(bool primary, bool enabled, float interaction, bool selected = false)
        {
            if (!enabled)
                return AppTheme.badge;

            Color normal = primary ? AppTheme.blue : selected ? AppTheme.selection : Color.White;
            Color hover = primary ? Color.FromArgb(0, 91, 224) : AppTheme.selection;
            Color pressed = primary ? Color.FromArgb(0, 77, 196) : Color.FromArgb(219, 234, 253);
            return interaction <= .65F ? Blend(normal, hover, interaction / .65F)
                : Blend(hover, pressed, (interaction - .65F) / .35F);
        }

        [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfo(uint action,
                                                        uint parameter,
                                                        [MarshalAs(UnmanagedType.Bool)] out bool value,
                                                        uint flags);
        #endregion
    }
}