using RustDeskHop.UI.Theme;

namespace RustDeskHop.UI.Controls
{
    internal sealed class Divider : Control
    {
        #region Constructors and Destructors
        public Divider()
        {
            Height = 1;
            TabStop = false;
            BackColor = AppTheme.line;
        }
        #endregion
    }
}