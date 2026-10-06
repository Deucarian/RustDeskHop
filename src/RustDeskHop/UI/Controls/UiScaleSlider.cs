using RustDeskHop.Models;
using RustDeskHop.UI.Theme;

namespace RustDeskHop.UI.Controls
{
    // This strip follows Windows DPI only, so the adjustment control never shrinks under the pointer.
    internal sealed class UiScaleSlider : UserControl
    {
        #region Constants and Fields
        private readonly TrackBar _slider = new TrackBar
        {
            Name = "UiScaleSlider",
            AccessibleName = "UI scale percentage",
            Minimum = UiScalePreference.MINIMUM,
            Maximum = UiScalePreference.MAXIMUM,
            SmallChange = 5,
            LargeChange = 25,
            TickStyle = TickStyle.None,
            AutoSize = false,
            TabIndex = 0
        };
        private readonly Label _label = new Label
        {
            Text = "UI size",
            TextAlign = ContentAlignment.MiddleLeft,
            Font = AppTheme.small,
            ForeColor = AppTheme.muted
        };
        private readonly Label _value = new Label
        {
            Name = "UiScaleValue",
            TextAlign = ContentAlignment.MiddleLeft,
            Font = AppTheme.small,
            ForeColor = AppTheme.ink
        };
        private readonly UiScaleState _state;
        private bool _updating;
        #endregion

        #region Constructors and Destructors
        internal UiScaleSlider(UiScaleState state)
        {
            _state = state;
            Name = "UiScaleControls";
            BackColor = Color.White;
            AutoScaleMode = AutoScaleMode.None;
            Dock = DockStyle.Bottom;
            TabStop = false;
            Controls.AddRange([_label, _slider, _value]);
            _slider.ValueChanged += (_, _) =>
            {
                if (!_updating)
                    _state.SetPercent(_slider.Value);
            };
            _slider.MouseUp += (_, _) => _state.Commit();
            _slider.KeyUp += (_, _) => _state.Commit();
            _slider.Leave += (_, _) => _state.Commit();
            _state.Changed += OnScaleChanged;
            OnScaleChanged(this, EventArgs.Empty);
        }
        #endregion

        #region Methods
        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (_slider is null)
                return;

            int Px(int value) => (int)Math.Round(value * DeviceDpi / 96F);
            Height = Px(40);
            _label.SetBounds(Px(20), 0, Px(52), Height);
            _slider.SetBounds(Px(72), Px(5), Px(160), Px(30));
            _value.SetBounds(Px(238), 0, Px(52), Height);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _state.Changed -= OnScaleChanged;
            base.Dispose(disposing);
        }

        private void OnScaleChanged(object? sender, EventArgs e)
        {
            _updating = true;
            _slider.Value = _state.Percent;
            _value.Text = $"{_state.Percent}%";
            _slider.AccessibleName = $"UI scale, {_state.Percent} percent";
            _slider.AccessibleDescription = $"{_state.Percent} percent. Default is 75 percent; 100 is the original size.";
            _updating = false;
        }
        #endregion
    }
}