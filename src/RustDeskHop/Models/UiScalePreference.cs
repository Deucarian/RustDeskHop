namespace RustDeskHop.Models
{
    internal static class UiScalePreference
    {
        #region Constants and Fields
        internal const int DEFAULT = 75, MINIMUM = 50, MAXIMUM = 150;
        #endregion

        #region Methods
        internal static int Normalize(int percent) => percent is >= MINIMUM and <= MAXIMUM ? percent : DEFAULT;
        #endregion
    }
}