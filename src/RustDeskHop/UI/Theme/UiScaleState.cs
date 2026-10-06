using RustDeskHop.Models;

namespace RustDeskHop.UI.Theme
{
    // One application-owned preference, shared by its windows. Never changes OS DPI or remote desktops.
    internal sealed class UiScaleState : IDisposable
    {
        #region Constants and Fields
        private readonly Dictionary<(Font Font, int Percent), Font> _fonts = [];
        private int _percent;
        #endregion

        #region Constructors and Destructors
        internal UiScaleState(int percent = UiScalePreference.DEFAULT)
        {
            _percent = UiScalePreference.Normalize(percent);
        }
        #endregion

        #region Delegates and Events
        internal event EventHandler? Changed;
        internal event EventHandler? Committed;
        #endregion

        #region Properties and Indexers
        internal int Percent => _percent;
        internal float Factor => _percent / 100F;
        #endregion

        #region Methods
        internal void SetPercent(int percent)
        {
            int normalized = UiScalePreference.Normalize(percent);
            if (_percent == normalized)
                return;

            _percent = normalized;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        internal void Commit() => Committed?.Invoke(this, EventArgs.Empty);

        internal Font FontFor(Font original)
        {
            if (_percent == 100)
                return original;

            if (!_fonts.TryGetValue((original, _percent), out Font? font))
            {
                font = new Font(original.FontFamily, original.Size * Factor, original.Style, original.Unit);
                _fonts.Add((original, _percent), font);
            }
            return font;
        }

        public void Dispose()
        {
            foreach (Font font in _fonts.Values)
            {
                font.Dispose();
            }
            _fonts.Clear();
        }
        #endregion
    }
}