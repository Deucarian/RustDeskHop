namespace RustDeskHop.UI.Theme
{
    internal sealed class InteractionMotion : IDisposable
    {
        #region Constants and Fields
        private readonly Control _owner;
        private readonly MotionTween _tween;
        private bool _hover;
        private bool _pressed;
        #endregion

        #region Constructors and Destructors
        internal InteractionMotion(Control owner, params Control[] children)
        {
            _owner = owner;
            _tween = new MotionTween(_ => owner.Invalidate());
            foreach (Control control in children.Prepend(owner))
            {
                control.MouseEnter += (_, _) => SetPointerState(true, _pressed);
                control.MouseLeave += (_, _) => SetPointerState(false, false);
                control.MouseDown += (_, e) => SetPointerState(_hover, e.Button == MouseButtons.Left);
                control.MouseUp += (_, _) => SetPointerState(_hover, false);
                control.GotFocus += (_, _) => Update();
                control.LostFocus += (_, _) => SetPointerState(_hover, false);
                control.KeyDown += (_, e) =>
                {
                    if (e.KeyCode is Keys.Space or Keys.Enter)
                    {
                        _pressed = true;
                        Update();
                    }
                };
                control.KeyUp += (_, _) => SetPointerState(_hover, false);
            }
            owner.EnabledChanged += (_, _) => Update();
            owner.VisibleChanged += (_, _) =>
            {
                if (!owner.Visible)
                {
                    _hover = false;
                    _pressed = false;
                    _tween.Reset(0);
                }
            };
        }
        #endregion

        #region Properties and Indexers
        internal float Value => _tween.Value;
        #endregion

        #region Methods
        public void Dispose() => _tween.Dispose();

        private void SetPointerState(bool hover, bool pressed)
        {
            _hover = hover;
            _pressed = pressed;
            Update();
        }

        private void Update() =>
            _tween.To(!_owner.Enabled ? 0 : _pressed ? 1 : _hover || _owner.ContainsFocus ? .65F : 0);
        #endregion
    }
}